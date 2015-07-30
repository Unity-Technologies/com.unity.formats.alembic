#include "pch.h"
#include "AlembicImporter.h"
#include "aiLogger.h"
#include "aiContext.h"
#include "aiObject.h"
#include "Schema/aiSchema.h"
#include "Schema/aiXForm.h"
#include "Schema/aiPolyMesh.h"
#include "Schema/aiCamera.h"
#include <limits>

class GlobalCache
{
public:

    struct ArchiveItem
    {
        Abc::IArchive archive;
        int refcount;
    };

    typedef std::map<int, aiContext*> ContextMap;
    typedef std::map<std::string, ArchiveItem> ArchiveMap;

public:

    static aiContext* GetContext(int uid)
    {
        ContextMap::iterator it = ms_instance.m_contexts.find(uid);

        if (it != ms_instance.m_contexts.end())
        {
            aiLogger::Info("Using already created context for gameObject with ID %d", uid);

            return it->second;
        }
        else
        {
            return 0;
        }
    }

    static bool RegisterContext(int uid, aiContext *ctx)
    {
        ContextMap::iterator it = ms_instance.m_contexts.find(uid);
        
        if (it != ms_instance.m_contexts.end())
        {
            return false;
        }
        else
        {
            aiLogger::Info("Register context for gameObject with ID %d", uid);

            ms_instance.m_contexts[uid] = ctx;

            if (!RefArchive(ctx->getPath()))
            {
                AddArchive(ctx->getPath(), ctx->getArchive());
            }

            return true;
        }
    }

    static void UnregisterContext(int uid)
    {
        ContextMap::iterator it = ms_instance.m_contexts.find(uid);
        
        if (it != ms_instance.m_contexts.end())
        {
            UnrefArchive(it->second->getPath());

            aiLogger::Info("Unregister context for gameObject with ID %d", uid);

            ms_instance.m_contexts.erase(it);
        }
    }

    static bool AddArchive(const std::string &path, Abc::IArchive archive)
    {
        if (!archive.valid())
        {
            return false;
        }

        ArchiveMap::iterator it = ms_instance.m_archives.find(path);
        
        if (it != ms_instance.m_archives.end())
        {
            return false;
        }
        else
        {
            aiLogger::Info("Add new alembic archive '%s'", path.c_str());
               
            ArchiveItem &item = ms_instance.m_archives[path];
            
            item.refcount = 1;
            item.archive = archive;

            return true;
        }
    }

    static Abc::IArchive RefArchive(const std::string &path)
    {
        ArchiveMap::iterator it = ms_instance.m_archives.find(path);
        
        if (it != ms_instance.m_archives.end())
        {
            aiLogger::Info("Reference alembic archive '%s'", path.c_str());

            it->second.refcount += 1;
            return it->second.archive;
        }
        else
        {
            return Abc::IArchive();
        }
    }

    static void UnrefArchive(const std::string &path)
    {
        ArchiveMap::iterator it = ms_instance.m_archives.find(path);
        
        if (it != ms_instance.m_archives.end())
        {
            aiLogger::Info("Unreference alembic archive '%s'", path.c_str());

            it->second.refcount -= 1;

            if (it->second.refcount <= 0)
            {
                aiLogger::Info("Remove alembic archive '%s'", path.c_str());

                ms_instance.m_archives.erase(it);
            }
        }
    }

private:

    GlobalCache()
    {
    }

    ~GlobalCache()
    {
        if (m_contexts.size())
        {
            aiLogger::Warning("%lu remaining context(s) registered", m_contexts.size());
        }

        for (ContextMap::iterator it=m_contexts.begin(); it!=m_contexts.end(); ++it)
        {
            delete it->second;
        }
        
        m_contexts.clear();
        m_archives.clear();
    }

private:

    ContextMap m_contexts;
    ArchiveMap m_archives;

    static GlobalCache ms_instance;
};

GlobalCache GlobalCache::ms_instance;

// ---

aiContext* aiContext::create(int uid)
{
    aiContext *ctx = GlobalCache::GetContext(uid);

    if (!ctx)
    {
        ctx = new aiContext(uid);
        
        GlobalCache::RegisterContext(uid, ctx);
    }

    return ctx;
}

void aiContext::destroy(aiContext* ctx)
{
    GlobalCache::UnregisterContext(ctx->getUid());

    delete ctx;
}

aiContext::aiContext(int uid)
    : m_path("")
    , m_uid(uid)
{
    m_timeRange[0] = 0;
    m_timeRange[1] = 0;
}

aiContext::~aiContext()
{
    waitTasks();
        
    for (auto n : m_nodes)
    {
        delete n;
    }

    m_nodes.clear();
}

Abc::IArchive aiContext::getArchive() const
{
    return m_archive;
}

const std::string& aiContext::getPath() const
{
    return m_path;
}

int aiContext::getUid() const
{
    return m_uid;
}

const aiConfig& aiContext::getConfig() const
{
    return m_config;
}

void aiContext::setConfig(const aiConfig &config)
{
    DebugLog("aiContext::setConfig: %s", config.toString().c_str());
    m_config = config;
}

void aiContext::gatherNodesRecursive(aiObject *n)
{
    m_nodes.push_back(n);

    abcObject &abc = n->getAbcObject();
    size_t numChildren = abc.getNumChildren();
    
    for (size_t i = 0; i < numChildren; ++i)
    {
        abcObject abcChild = abc.getChild(i);
        aiObject *child = new aiObject(this, abcChild);
        n->addChild(child);
        gatherNodesRecursive(child);
    }
}

void aiContext::destroyObject(aiObject *obj)
{
    DebugLog("aiObject::destroyObject()");

    if (!obj)
    {
        return;
    }

    aiObject *parent = obj->getParent();
    
    std::vector<aiObject*>::iterator it = m_nodes.end();

    if (destroyObject(obj, m_nodes.begin(), it))
    {
        // note: at this point obj has already been destroyed
        //       remove it from its parent children list
        parent->removeChild(obj);
    }
}

bool aiContext::destroyObject(aiObject *obj,
                              std::vector<aiObject*>::iterator searchFrom,
                              std::vector<aiObject*>::iterator &next)
{
    std::vector<aiObject*>::iterator it = std::find(searchFrom, m_nodes.end(), obj);
    std::vector<aiObject*>::iterator nit;
    
    if (it != m_nodes.end())
    {
        size_t fromIndex = searchFrom - m_nodes.begin();

        // it now points to next element after found object
        // (its first child or next sibling if it hasn't got any child)
        it = m_nodes.erase(it);

        // also destroy all the childrens
        uint32_t numChildren = obj->getNumChildren();
        
        for (uint32_t c=0; c<numChildren; ++c)
        {
            destroyObject(obj->getChild(c), it, nit);
            // don't need to call removeChild on obj here as it is the be deleted
            it = nit;
        }

        // remove from parent nodes list

        delete obj;

        next = (m_nodes.begin() + fromIndex);
        return true;
    }
    else
    {
        next = searchFrom;
        return false;
    }
}

void aiContext::reset()
{
    DebugLog("aiContext::reset()");
    
    // just in case
    waitTasks();

    for (auto n : m_nodes)
    {
        delete n;
    }

    m_nodes.clear();

    GlobalCache::UnrefArchive(m_path);
    
    m_path = "";
    m_archive.reset();

    m_timeRange[0] = 0.0f;
    m_timeRange[1] = 0.0f;
}

std::string aiContext::normalizePath(const char *inPath) const
{
    std::string path;

    if (inPath != nullptr)
    {
        path = inPath;

        #ifdef _WIN32
        
        size_t n = path.length();
        
        for (size_t i=0; i<n; ++i)
        {
            char c = path[i];
            if (c == '\\')
            {
                path[i] = '/';
            }
            else if (c >= 'A' && c <= 'Z')
            {
                path[i] = 'a' + (c - 'A');
            }
        }
        
        #endif
    }

    return path;
}

bool aiContext::load(const char *inPath)
{
    std::string path = normalizePath(inPath);

    DebugLog("aiContext::load: '%s'", path.c_str());

    if (path == m_path && m_archive)
    {
        aiLogger::Info("Context already loaded for gameObject with id %d", m_uid);
        return true;
    }

    aiLogger::Info("Alembic file path changed from '%s' to '%s'. Reset context.", m_path.c_str(), path.c_str());
    aiLogger::Indent(1);

    reset();

    if (path.length() == 0)
    {
        aiLogger::Unindent(1);
        return false;
    }

    m_path = path;
    m_archive = GlobalCache::RefArchive(m_path);
    
    if (!m_archive.valid())
    {
        aiLogger::Info("Archive '%s' not yet opened", inPath);

        try
        {
            DebugLog("Trying to open AbcCoreOgawa::ReadArchive...");
            m_archive = Abc::IArchive(AbcCoreOgawa::ReadArchive(), path);
        }
        catch (Alembic::Util::Exception e)
        {
            DebugLog("Failed (%s)", e.what());

            try
            {
                DebugLog("Trying to open AbcCoreHDF5::ReadArchive...");
                m_archive = Abc::IArchive(AbcCoreHDF5::ReadArchive(), path);
            }
            catch (Alembic::Util::Exception e)
            {
                DebugLog("Failed (%s)", e.what());
            }
        }
    }
    else
    {
        aiLogger::Info("Archive '%s' already opened", inPath);
    }

    if (m_archive.valid())
    {
        abcObject abcTop = m_archive.getTop();
        aiObject *top = new aiObject(this, abcTop);
        gatherNodesRecursive(top);

        m_timeRange[0] = std::numeric_limits<double>::max();
        m_timeRange[1] = -std::numeric_limits<double>::max();

        for (unsigned int i=0; i<m_archive.getNumTimeSamplings(); ++i)
        {
            AbcCoreAbstract::TimeSamplingPtr ts = m_archive.getTimeSampling(i);

            AbcCoreAbstract::TimeSamplingType tst = ts->getTimeSamplingType();

            // Note: alembic guaranties we have at least one stored time

            if (tst.isCyclic() || tst.isUniform())
            {
                size_t numCycles = (m_archive.getMaxNumSamplesForTimeSamplingIndex(i) / tst.getNumSamplesPerCycle());

                m_timeRange[0] = ts->getStoredTimes()[0];
                m_timeRange[1] = m_timeRange[0] + (numCycles - 1) * tst.getTimePerCycle();
            }
            else if (tst.isAcyclic())
            {
                m_timeRange[0] = ts->getSampleTime(0);
                m_timeRange[1] = ts->getSampleTime(ts->getNumStoredTimes() - 1);
            }
        }

        if (m_timeRange[0] > m_timeRange[1])
        {
            m_timeRange[0] = 0.0;
            m_timeRange[1] = 0.0;
        }

        DebugLog("Succeeded");

        GlobalCache::AddArchive(m_path, m_archive);

        aiLogger::Unindent(1);

        return true;
    }
    else
    {
        aiLogger::Error("Invalid archive '%s'", inPath);

        m_path = "";
        m_archive.reset();

        m_timeRange[0] = 0.0;
        m_timeRange[1] = 0.0;

        aiLogger::Unindent(1);

        return false;
    }
}

float aiContext::getStartTime() const
{
    return float(m_timeRange[0]);
}

float aiContext::getEndTime() const
{
    return float(m_timeRange[1]);
}

aiObject* aiContext::getTopObject()
{
    return m_nodes.empty() ? nullptr : m_nodes.front();
}

void aiContext::updateSamples(float time)
{
    if (m_config.useThreads)
    {
        DebugLog("aiContext::updateSamples() [threaded]");
        
        for (aiObject *obj : m_nodes)
        {
            obj->readConfig();
        }
        
        for (aiObject *obj : m_nodes)
        {
            enqueueTask([obj, time]() {
                obj->updateSample(time);
            });
        }

        waitTasks();

        for (aiObject *obj : m_nodes)
        {
            obj->notifyUpdate();
        }
    }
    else
    {
        DebugLog("aiContext::updateSamples()");
        
        for (auto &e : m_nodes)
        {
            e->updateSample(time);
        }
    }
}

void aiContext::enqueueTask(const std::function<void()> &task)
{
    m_tasks.run(task);
}

void aiContext::waitTasks()
{
    m_tasks.wait();
}
