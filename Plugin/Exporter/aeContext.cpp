#include "pch.h"
#include "AlembicExporter.h"
#include "aeContext.h"
#include "aeObject.h"

aeContext::aeContext(const aeConfig &conf)
    : m_config(conf)
    , m_default_timesampling_index(0)
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
        if (m_config.timeSamplingType == aeTimeSamplingType_Acyclic) {
            for (int i = m_default_timesampling_index; i < (int)m_timesamplings.size(); ++i) {
                auto ts = Abc::TimeSampling(Abc::TimeSamplingType(Abc::TimeSamplingType::kAcyclic), m_timesamplings[i]);
                *m_archive.getTimeSampling(i) = ts;
            }
        }
    }

    m_node_top.reset();
    m_timesamplings.clear();
    m_archive.reset(); // flush archive
    m_default_timesampling_index = 0;
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

    Abc::TimeSampling ts = Abc::TimeSampling(abcChrono(1.0f / m_config.frameRate), abcChrono(m_config.startTime));
    m_default_timesampling_index = m_archive.addTimeSampling(ts);
    m_timesamplings.resize(m_default_timesampling_index + 1);

    auto *top = new AbcGeom::OObject(m_archive, AbcGeom::kTop, m_default_timesampling_index);
    m_node_top.reset(new aeObject(this, nullptr, top, m_default_timesampling_index));
    return true;
}

const aeConfig& aeContext::getConfig() const
{
    return m_config;
}

uint32_t aeContext::getDefaultTimeSaplingIndex() const
{
    return m_default_timesampling_index;
}

aeObject* aeContext::getTopObject()
{
    return m_node_top.get();
}

uint32_t aeContext::addTimeSampling(float start_time)
{
    Abc::TimeSampling ts = Abc::TimeSampling(abcChrono(1.0f / m_config.frameRate), abcChrono(start_time));
    uint32_t r = m_archive.addTimeSampling(ts);
    m_timesamplings.resize(r + 1);
    return r;
}

void aeContext::setTime(float time, uint32_t tsi)
{
    if (tsi == 0) { tsi = getDefaultTimeSaplingIndex(); }

    auto &ts = m_timesamplings[tsi];
    if (ts.empty() || ts.back() != time) {
        ts.push_back(time);
    }
}
