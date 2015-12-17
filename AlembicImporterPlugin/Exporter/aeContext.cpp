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
        // set time sampling and flush
        Abc::TimeSampling ts = Abc::TimeSampling(
            Abc::TimeSamplingType(Abc::TimeSamplingType::kAcyclic), m_times);
        m_archive.addTimeSampling(ts);
    }

    m_node_top.reset();
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

    m_node_top.reset(new aeObject(this, nullptr, new AbcGeom::OObject(m_archive, AbcGeom::kTop)));
    return true;
}

const aeConfig& aeContext::getConfig() const
{
    return m_config;
}

aeObject* aeContext::getTopObject()
{
    return m_node_top.get();
}

void aeContext::setTime(float time)
{
    m_times.push_back(time);
}
