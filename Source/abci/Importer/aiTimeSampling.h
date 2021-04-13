#pragma once

class aiTimeSampling
{
 public:
    virtual ~aiTimeSampling();
    virtual void getTimeRange(double& begin, double& end) const = 0;
    virtual size_t getSampleCount() const = 0;
    virtual double getTime(uint64_t sample_index) const = 0;
};
using aiTimeSamplingPtr = std::shared_ptr<aiTimeSampling>;

aiTimeSampling* aiCreateTimeSampling(Abc::IArchive& archive, int index);
