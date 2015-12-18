#include "pch.h"
#include "AlembicExporter.h"
#include "aeContext.h"
#include "aeObject.h"

aeContext::aeContext(const aeConfig &conf)
    : m_config(conf)
    , m_time_sampling_index(0)
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
        if (m_config.timeSamplingType == aeTypeSamplingType_Acyclic) {
            Abc::TimeSampling ts = Abc::TimeSampling(Abc::TimeSamplingType(Abc::TimeSamplingType::kAcyclic), m_times);
            *m_archive.getTimeSampling(m_time_sampling_index) = ts;
        }
    }

    m_node_top.reset();
    m_times.clear();
    m_archive.reset(); // flush archive
    m_time_sampling_index = 0;
}

bool aeContext::openArchive(const char *path)
{
    reset();

    aeDebugLog("aeContext::openArchive() %s", path);
    try {
        if (m_config.archiveType == aeArchiveType_HDF5) {
            m_archive = Abc::OArchive(Alembic::AbcCoreHDF5::WriteArchive(), path);
        }
        else if (m_config.archiveType == aeArchiveType_Ogawa) {
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

    Abc::TimeSampling ts = Abc::TimeSampling(abcChrono(m_config.timePerSample), abcChrono(m_config.startTime));
    m_time_sampling_index = m_archive.addTimeSampling(ts);

    m_node_top.reset(new aeObject(this, nullptr, new AbcGeom::OObject(m_archive, AbcGeom::kTop, getTimeSaplingIndex())));
    return true;
}

const aeConfig& aeContext::getConfig() const
{
    return m_config;
}

uint32_t aeContext::getTimeSaplingIndex() const
{
    return m_time_sampling_index;
}

aeObject* aeContext::getTopObject()
{
    return m_node_top.get();
}

void aeContext::setTime(float time)
{
    m_times.push_back(time);
}
