#include "pch.h"
#include "aeInternal.h"
#include "aeContext.h"
#include "aeObject.h"

aeContext::aeContext()
{
    DebugLog("aeContext::aeContext()");
}

aeContext::~aeContext()
{
    DebugLog("aeContext::~aeContext()");
    reset();
}

void aeContext::reset()
{
    waitAsync();

    if (m_archive != nullptr)
    {
        if (m_config.time_sampling_type == aeTimeSamplingType::Uniform)
        {
            // start time in default time sampling maybe changed.
            for (int i = 1; i < (int)m_timesamplings.size(); ++i)
            {
                auto ts = Abc::TimeSampling(abcChrono(1.0f / m_config.frame_rate), m_timesamplings[i]->start_time);
                *m_archive.getTimeSampling(i) = ts;
            }
        }
        else if (m_config.time_sampling_type == aeTimeSamplingType::Cyclic)
        {
            for (int i = 1; i < (int)m_timesamplings.size(); ++i)
            {
                auto& t = *m_timesamplings[i];
                if (!t.times.empty())
                {
                    auto ts =
                        Abc::TimeSampling(Abc::TimeSamplingType((uint32_t)t.times.size(), t.times.back()), t.times);
                    *m_archive.getTimeSampling(i) = ts;
                }
            }
        }
        else if (m_config.time_sampling_type == aeTimeSamplingType::Acyclic)
        {
            for (int i = 1; i < (int)m_timesamplings.size(); ++i)
            {
                auto& t = *m_timesamplings[i];
                auto ts = Abc::TimeSampling(Abc::TimeSamplingType(Abc::TimeSamplingType::kAcyclic), t.times);
                *m_archive.getTimeSampling(i) = ts;
            }
        }
    }

    m_node_top.reset();
    m_timesamplings.clear();
    m_archive.reset(); // flush archive
}

void aeContext::setConfig(const aeConfig& conf)
{
    m_config = conf;
}

bool aeContext::openArchive(const char* path)
{
    reset();

    DebugLog("aeContext::openArchive() %s", path);
    try
    {
        m_archive = Abc::OArchive(Alembic::AbcCoreOgawa::WriteArchive(), path);
    }
    catch (Alembic::Util::Exception e)
    {
        DebugLog("Failed (%s)", e.what());
        return false;
    }

    // reserve 'default' time sampling. update it later in reset()
    auto tsi = addTimeSampling(0.0);

    auto* top = new AbcGeom::OObject(m_archive, AbcGeom::kTop, tsi);
    m_node_top.reset(new aeObject(this, nullptr, top, tsi));
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

uint32_t aeContext::getNumTimeSampling() const
{
    return (uint32_t)m_timesamplings.size();
}

abcTimeamplingPtr aeContext::getTimeSampling(uint32_t i)
{
    return m_archive.getTimeSampling(i);
}

aeTimeSamplingData& aeContext::getTimeSamplingData(uint32_t i)
{
    return *m_timesamplings[i];
}

uint32_t aeContext::addTimeSampling(double start_time)
{
    // add dummy data. it will be updated in aeContext::reset()
    auto ts = abcTimeampling(abcChrono(1.0f / 30.0f), abcChrono(start_time));
    auto tsi = m_archive.addTimeSampling(ts);
    while (m_timesamplings.size() < tsi + 1)
    {
        m_timesamplings.emplace_back(new aeTimeSamplingData());
    }
    m_timesamplings[tsi]->start_time = start_time;
    return tsi;
}

void aeContext::addTime(double time, uint32_t tsi)
{
    // if tsi==-1, add time to all time samplings
    if (tsi == -1)
    {
        for (size_t i = 1; i < m_timesamplings.size(); ++i)
        {
            auto& ts = *m_timesamplings[i];
            if (ts.times.empty() || ts.times.back() != time)
            {
                ts.times.push_back(time);
            }
        }
    }
    else
    {
        auto& ts = *m_timesamplings[tsi];
        if (ts.times.empty() || ts.times.back() != time)
        {
            ts.times.push_back(time);
        }
    }
}

void aeContext::markFrameBegin()
{
    waitAsync();
}

void aeContext::markFrameEnd()
{
    // kick async tasks
    m_async_task_future = std::async(std::launch::async, [this]()
    {
      for (auto& task : m_async_tasks)
          task();
      m_async_tasks.clear();
    });
}

void aeContext::addAsync(const std::function<void()>& task)
{
    m_async_tasks.push_back(task);
}

void aeContext::waitAsync()
{
    if (m_async_task_future.valid())
    {
        m_async_task_future.wait();
    }
}
