#include "pch.h"
#include "aiInternal.h"
#include "aiContext.h"
#include "aiObject.h"
#include <limits>


std::string ToString(const aiConfig &v)
{
    std::ostringstream oss;

    oss << "{swapHandedness: " << (v.swap_handedness ? "true" : "false");
    oss << ", swapFaceWinding: " << (v.swap_face_winding ? "true" : "false");
    oss << ", normalsMode: " << (v.normals_mode == aiNormalsMode::ReadFromFile
        ? "read_from_file"
        : (v.normals_mode == aiNormalsMode::ComputeIfMissing
            ? "compute_if_missing"
            : (v.normals_mode == aiNormalsMode::AlwaysCompute
                ? "always_compute"
                : "ignore")));
    oss << ", tangentsMode: " << (v.tangents_mode == aiTangentsMode::None
        ? "none"
        : (v.tangents_mode == aiTangentsMode::Compute
            ? "smooth"
            : "split"));
    oss << ", aspectRatio: " << v.aspect_ratio;

    return oss.str();
}

aiContextManager aiContextManager::s_instance;

aiContext* aiContextManager::getContext(int uid)
{
    auto it = s_instance.m_contexts.find(uid);
if (it != s_instance.m_contexts.end()) {
        DebugLog("Using already created context for gameObject with ID %d", uid);
        return it->second.get();
    }

    auto ctx = new aiContext(uid);
    s_instance.m_contexts[uid].reset(ctx);
    DebugLog("Register context for gameObject with ID %d", uid);
    return ctx;
}

void aiContextManager::destroyContext(int uid)
{
    auto it = s_instance.m_contexts.find(uid);
    if (it != s_instance.m_contexts.end()) {
        DebugLog("Unregister context for gameObject with ID %d", uid);
        s_instance.m_contexts.erase(it);
    }
}

void aiContextManager::destroyContextsWithPath(const char* assetPath)
{
    auto path = aiContext::normalizePath(assetPath);
    for (auto it = s_instance.m_contexts.begin(); it != s_instance.m_contexts.end(); ++it) {
        if (it->second->getPath() == path) {
            DebugLog("Unregister context for gameObject with ID %s", it->second->getPath().c_str());
            s_instance.m_contexts.erase(it);
        }
    }
}

aiContextManager::~aiContextManager()
{
    if (m_contexts.size()) {
        DebugWarning("%lu remaining context(s) registered", m_contexts.size());
    }
    m_contexts.clear();
}



aiContext::aiContext(int uid)
    : m_uid(uid)
{
}

aiContext::~aiContext()
{
    if (m_async_load.valid())
        m_async_load.wait();

    m_top_node.reset();
    m_archive.reset();
}

Abc::IArchive aiContext::getArchive() const
{
    return m_archive;
}

const std::string& aiContext::getPath() const
{
    return m_path;
}


int aiContext::getTimeRangeCount() const
{
    return (int)m_time_ranges.size();
}
void aiContext::getTimeRange(int tsi, aiTimeRange & dst) const
{
    if (tsi >= 0 && tsi < m_time_ranges.size()) {
        dst = m_time_ranges[tsi];
    }
    else {
        dst = m_time_range_unified;
    }
}

int aiContext::getTimeSamplingCount()
{
    return (int)m_archive.getNumTimeSamplings();
}

void aiContext::getTimeSampling(int i, aiTimeSamplingData& dst)
{
    auto ts = m_archive.getTimeSampling(i);
    auto tst = ts->getTimeSamplingType();

    dst.numTimes = (int)ts->getNumStoredTimes();
    if (tst.isUniform() || tst.isCyclic()) {
        int numCycles = int(m_archive.getMaxNumSamplesForTimeSamplingIndex(i) / tst.getNumSamplesPerCycle());

        dst.type = tst.isUniform() ? aiTimeSamplingType::Uniform : aiTimeSamplingType::Cyclic;
        dst.interval = (float)tst.getTimePerCycle();
        dst.start_time = (float)ts->getStoredTimes()[0];
        dst.end_time = dst.start_time + dst.interval * (numCycles - 1);
        dst.numTimes = (int)ts->getNumStoredTimes();
        dst.times = const_cast<double*>(&ts->getStoredTimes()[0]);
    }
    else if (tst.isAcyclic()) {
        dst.type = aiTimeSamplingType::Acyclic;
        dst.start_time = (float)ts->getSampleTime(0);
        dst.end_time = (float)ts->getSampleTime(ts->getNumStoredTimes() - 1);
        dst.numTimes = (int)ts->getNumStoredTimes();
        dst.times = const_cast<double*>(&ts->getStoredTimes()[0]);
    }
}

void aiContext::copyTimeSampling(int i, aiTimeSamplingData& dst)
{
    int dst_numSamples = dst.numTimes;
    double *dst_samples = dst.times;
    getTimeSampling(i, dst);

    if (dst.type == aiTimeSamplingType::Acyclic) {
        const auto& times = m_archive.getTimeSampling(i)->getStoredTimes();
        if (dst_samples && dst_numSamples >= (int)times.size()) {
            // memcpy() is way faster than std::copy() on VC...
            memcpy(dst.times, &times[0], sizeof(times[0])*times.size());
        }
    }
}

int aiContext::getTimeSamplingIndex(Abc::TimeSamplingPtr ts)
{
    int n = m_archive.getNumTimeSamplings();
    for (int i = 0; i < n; ++i) {
        if (m_archive.getTimeSampling(i) == ts) {
            return i;
        }
    }
    return 0;
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
    DebugLog("aiContext::setConfig: %s", ToString(config).c_str());
    m_config = config;
}

void aiContext::gatherNodesRecursive(aiObject *n)
{
    abcObject &abc = n->getAbcObject();
    size_t numChildren = abc.getNumChildren();
    
    for (size_t i = 0; i < numChildren; ++i)
    {
        aiObject *child = n->newChild(abc.getChild(i));
        gatherNodesRecursive(child);
    }
}

void aiContext::reset()
{
    DebugLog("aiContext::reset()");

    m_top_node.reset();
    
    m_path = "";
    m_archive.reset();

    m_time_range_unified = {};
    m_time_ranges.clear();
}

std::string aiContext::normalizePath(const char *inPath)
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
    if (path == m_path && m_archive) {
        DebugLog("Context already loaded for gameObject with id %d", m_uid);
        return true;
    }

    DebugLog("Alembic file path changed from '%s' to '%s'. Reset context.", m_path.c_str(), path.c_str());
    aiLogger::Indent(1);

    reset();

    if (path.length() == 0) {
        aiLogger::Unindent(1);
        return false;
    }

    m_path = path;
    
    if (!m_archive.valid()) {
        DebugLog("Archive '%s' not yet opened", inPath);

        try {
            DebugLog("Trying to open AbcCoreOgawa::ReadArchive...");
            m_archive = Abc::IArchive(AbcCoreOgawa::ReadArchive(std::thread::hardware_concurrency()), path);
        }
        catch (Alembic::Util::Exception e) {
            DebugLog("Failed (%s)", e.what());
            try {
                DebugLog("Trying to open AbcCoreHDF5::ReadArchive...");
                m_archive = Abc::IArchive(AbcCoreHDF5::ReadArchive(), path);
            }
            catch (Alembic::Util::Exception e2) {
                DebugLog("Failed (%s)", e2.what());
            }
        }
    }
    else {
        DebugLog("Archive '%s' already opened", inPath);
    }

    if (m_archive.valid()) {
        abcObject abcTop = m_archive.getTop();
        m_top_node.reset(new aiObject(this, nullptr, abcTop));
        gatherNodesRecursive(m_top_node.get());

        m_time_range_unified = {};
        m_time_ranges.clear();
        auto num_time_samplings = (int)m_archive.getNumTimeSamplings();
        if (num_time_samplings > 1) {
            aiTimeRange tr;
            tr.start_time = std::numeric_limits<float>::max();
            tr.end_time = -std::numeric_limits<float>::max();

            for (int i = 1; i < num_time_samplings; ++i) {
                auto ts = m_archive.getTimeSampling(i);
                auto tst = ts->getTimeSamplingType();

                // Note: alembic guaranties we have at least one stored time
                if (tst.isCyclic() || tst.isUniform()) {
                    auto max_num_samples = m_archive.getMaxNumSamplesForTimeSamplingIndex(i);
                    auto samples_per_cycle = tst.getNumSamplesPerCycle();
                    int num_cycles = int(max_num_samples / samples_per_cycle);

                    if (tst.isUniform()) tr.type = aiTimeSamplingType::Uniform;
                    else if (tst.isCyclic()) tr.type = aiTimeSamplingType::Cyclic;
                    tr.start_time = ts->getStoredTimes()[0];
                    tr.end_time = tr.start_time + (num_cycles - 1) * tst.getTimePerCycle();
                    tr.frame_count = num_cycles;
                }
                else if (tst.isAcyclic()) {
                    tr.type = aiTimeSamplingType::Acyclic;
                    tr.start_time = ts->getSampleTime(0);
                    tr.end_time = ts->getSampleTime(ts->getNumStoredTimes() - 1);
                    tr.frame_count = (int)ts->getNumStoredTimes();
                }

                if (tr.start_time > tr.end_time) {
                    tr.start_time = 0.0;
                    tr.end_time = 0.0;
                }
                m_time_ranges.push_back(tr);
            }

            m_time_range_unified = m_time_ranges.front();
            for (size_t i = 1; i < m_time_ranges.size(); ++i) {
                auto& tr = m_time_ranges[i];
                if (m_time_range_unified.type != tr.type)
                    m_time_range_unified.type = aiTimeSamplingType::Mixed;
                m_time_range_unified.start_time = std::min(m_time_range_unified.start_time, tr.start_time);
                m_time_range_unified.end_time = std::max(m_time_range_unified.end_time, tr.end_time);
                m_time_range_unified.frame_count = std::max(m_time_range_unified.frame_count, tr.frame_count);
            }
        }

        DebugLog("Succeeded");
        aiLogger::Unindent(1);
        return true;
    }
    else {
        aiLogger::Error("Invalid archive '%s'", inPath);
        aiLogger::Unindent(1);
        reset();
        return false;
    }
}

aiObject* aiContext::getTopObject() const
{
    return m_top_node.get();
}

void aiContext::updateSamples(double time)
{
    auto ss = aiTimeToSampleSelector(time);
    eachNodes([ss](aiObject& o) {
        o.updateSample(ss);
    });

    // process async tasks
    if (!m_load_tasks.empty()) {
        if (m_async_load.valid())
            m_async_load.wait();

        m_async_load = std::async(std::launch::async, [this]() {
            for (auto t : m_load_tasks) {
                if(t->task_read)
                    t->task_read();

                if (t->task_cook) {
                    t->async_cook = std::async(std::launch::async, [t]() {
                        t->task_cook();
                        {
                            std::lock_guard<std::mutex> lock(t->m_mutex);
                            t->completed = true;
                        }
                        t->notify_completed.notify_all();
                    });
                }
            }
            m_load_tasks.clear();
        });
    }
}

void aiContext::queueTask(aiLoadTaskData& task)
{
    task.completed = false;
    m_load_tasks.push_back(&task);
}

void aiLoadTaskData::wait()
{
    if (!completed) {
        std::unique_lock<std::mutex> lock(m_mutex);
        notify_completed.wait(lock, [this] { return completed; });
    }
}
