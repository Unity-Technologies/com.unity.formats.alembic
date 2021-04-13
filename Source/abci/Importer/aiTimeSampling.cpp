#include "pch.h"
#include "aiInternal.h"
#include "aiTimeSampling.h"

class aiTimeSamplingUniform : public aiTimeSampling
{
 public:
    aiTimeSamplingUniform(double start, double interval, size_t sample_count);
    void getTimeRange(double& begin, double& end) const override;
    size_t getSampleCount() const override;
    double getTime(uint64_t sample_index) const override;

 private:
    double m_start = 0.0;
    double m_interval = 1.0;
    size_t m_sample_count = 0;
};

class aiTimeSamplingAcyclic : public aiTimeSampling
{
 public:
    aiTimeSamplingAcyclic(Abc::TimeSamplingPtr tsp);
    void getTimeRange(double& begin, double& end) const override;
    size_t getSampleCount() const override;
    double getTime(uint64_t sample_index) const override;

 private:
    Abc::TimeSamplingPtr m_tsp;
};

class aiTimeSamplingCyclic : public aiTimeSampling
{
 public:
    aiTimeSamplingCyclic(double start, double interval, size_t m_cycle_count, Abc::TimeSamplingPtr tsp);
    void getTimeRange(double& begin, double& end) const override;
    size_t getSampleCount() const override;
    double getTime(uint64_t sample_index) const override;

 private:
    double m_start = 0.0;
    double m_interval = 1.0;
    size_t m_cycle_count = 0;
    size_t m_samples_per_cycle = 0;
    Abc::TimeSamplingPtr m_tsp;
};

aiTimeSampling* aiCreateTimeSampling(Abc::IArchive& archive, int index)
{
    auto ts = archive.getTimeSampling(index);
    auto tst = ts->getTimeSamplingType();
    if (tst.isUniform() || tst.isCyclic())
    {
        auto start = ts->getStoredTimes()[0];
        auto max_num_samples = archive.getMaxNumSamplesForTimeSamplingIndex(index);
        auto samples_per_cycle = tst.getNumSamplesPerCycle();
        auto time_per_cycle = tst.getTimePerCycle();
        size_t num_cycles = int(max_num_samples / samples_per_cycle);

        if (tst.isUniform())
            return new aiTimeSamplingUniform(start, time_per_cycle, num_cycles);
        else if (tst.isCyclic())
            return new aiTimeSamplingCyclic(start, time_per_cycle, num_cycles, ts);
    }
    else if (tst.isAcyclic())
    {
        return new aiTimeSamplingAcyclic(ts);
    }
    return nullptr;
}

aiTimeSampling::~aiTimeSampling()
{
}

aiTimeSamplingUniform::aiTimeSamplingUniform(double start, double interval, size_t sample_count)
{
    m_start = start;
    m_interval = interval;
    m_sample_count = sample_count;
}

void aiTimeSamplingUniform::getTimeRange(double& begin, double& end) const
{
    begin = m_start;
    end = m_sample_count > 0 ? m_start + m_interval * (m_sample_count - 1) : begin;
}

size_t aiTimeSamplingUniform::getSampleCount() const
{
    return m_sample_count;
}

double aiTimeSamplingUniform::getTime(uint64_t sample_index) const
{
    return m_start + m_interval * std::min<uint64_t>(sample_index, m_sample_count);
}

aiTimeSamplingAcyclic::aiTimeSamplingAcyclic(Abc::TimeSamplingPtr tsp)
{
    m_tsp = tsp;
}

void aiTimeSamplingAcyclic::getTimeRange(double& begin, double& end) const
{
    auto& st = m_tsp->getStoredTimes();
    begin = st.front();
    end = st.back();
}

size_t aiTimeSamplingAcyclic::getSampleCount() const
{
    return m_tsp->getStoredTimes().size();
}

double aiTimeSamplingAcyclic::getTime(uint64_t sample_index) const
{
    auto& st = m_tsp->getStoredTimes();
    sample_index = std::min<uint64_t>(sample_index, st.size() - 1);
    return st[(size_t)sample_index];
}

aiTimeSamplingCyclic::aiTimeSamplingCyclic(double start, double interval, size_t cycle_count, Abc::TimeSamplingPtr tsp)
{
    m_start = start;
    m_interval = interval;
    m_cycle_count = cycle_count;
    m_samples_per_cycle = tsp->getTimeSamplingType().getNumSamplesPerCycle();
    m_tsp = tsp;
}

void aiTimeSamplingCyclic::getTimeRange(double& begin, double& end) const
{
    auto& st = m_tsp->getStoredTimes();
    begin = m_start + (st.front() - m_interval);
    end = m_start + m_interval * m_cycle_count + (st.back() - m_interval);
}

size_t aiTimeSamplingCyclic::getSampleCount() const
{
    return m_cycle_count * m_samples_per_cycle;
}

double aiTimeSamplingCyclic::getTime(uint64_t sample_index) const
{
    uint64_t cycle_index = sample_index / m_samples_per_cycle;
    uint64_t time_index = sample_index % m_samples_per_cycle;
    auto& st = m_tsp->getStoredTimes();
    return m_start + m_interval * cycle_index + (st[time_index] - m_interval);
}
