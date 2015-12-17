#include "pch.h"
#include "AlembicExporter.h"
#include "aeContext.h"
#include "aeObject.h"

aeContext::aeContext(const aeConfig &conf)
    : m_config(conf)
{
    aeDebugLog("aeContext::aeContext()");
}

aeContext::~aeContext()
{
    aeDebugLog("aeContext::~aeContext()");
    reset();
}

void aeContext::reset()
{
    if (m_archive != nullptr) {
        try {
            // set time sampling and flush
            Abc::TimeSampling ts = Abc::TimeSampling(
                Abc::TimeSamplingType(Abc::TimeSamplingType::kAcyclic), m_times);
            m_archive.addTimeSampling(ts);
            m_archive.reset();
        }
        catch (Alembic::Util::Exception e)
        {
            aeDebugLog("Failed (%s)", e.what());
        }
    }

    for (auto n : m_nodes) { delete n; }
    m_nodes.clear();
    m_times.clear();
    m_archive.reset();
}

bool aeContext::openArchive(const char *path)
{
    reset();

    aeDebugLog("aeContext::openArchive() %s", path);
    try {
        if (m_config.archive_type == aeArchiveType_HDF5) {
            m_archive = Abc::OArchive(Alembic::AbcCoreHDF5::WriteArchive(), path);
        }
        else if (m_config.archive_type == aeArchiveType_Ogawa) {
            m_archive = Abc::OArchive(Alembic::AbcCoreOgawa::WriteArchive(), path);
        }
        else {
            return false;
        }
    }
    catch (Alembic::Util::Exception e) {
        aeDebugLog("Failed (%s)", e.what());
        return false;
    }

    aeObject *top = new aeObject(this, AbcGeom::OObject(m_archive, AbcGeom::kTop), "");
    m_nodes.push_back(top);
    return true;
}

aeObject* aeContext::getTopObject()
{
    return m_nodes.empty() ? nullptr : m_nodes.front();
}

aeObject* aeContext::createObject(aeObject *parent, const char *name)
{
    aeObject *child = new aeObject(parent, name);
    parent->addChild(child);
    m_nodes.push_back(child);
    return child;
}

void aeContext::destroyObject(aeObject *obj)
{
    aeObject *parent = obj->getParent();

    auto it = m_nodes.end();
    if (destroyObject(obj, m_nodes.begin(), it))
    {
        // note: at this point obj has already been destroyed
        //       remove it from its parent children list
        parent->removeChild(obj);
    }
}

bool aeContext::destroyObject(aeObject *obj, NodeIter searchFrom, NodeIter &next)
{
    NodeIter it = std::find(searchFrom, m_nodes.end(), obj);
    NodeIter nit;

    if (it != m_nodes.end())
    {
        size_t fromIndex = searchFrom - m_nodes.begin();

        // it now points to next element after found object
        // (its first child or next sibling if it hasn't got any child)
        it = m_nodes.erase(it);

        // also destroy all the childrens
        uint32_t numChildren = obj->getNumChildren();

        for (uint32_t c = 0; c < numChildren; ++c)
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

void aeContext::setTime(float time)
{
    m_times.push_back(time);
}
