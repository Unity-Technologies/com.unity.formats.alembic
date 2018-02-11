#pragma once
#include "aiAsync.h"

class aiCameraSample : public aiSample
{
using super = aiSample;
public:
    aiCameraSample(aiCamera *schema);

    void getData(aiCameraData &dst) const;

public:
    AbcGeom::CameraSample m_sample, m_next_sample;
    aiCameraData m_data;
};


struct aiCameraTraits
{
    using SampleT = aiCameraSample;
    using AbcSchemaT = AbcGeom::ICameraSchema;
};


class aiCamera : public aiTSchema<aiCameraTraits>
{
using super = aiTSchema<aiCameraTraits>;
public:
    aiCamera(aiObject *parent, const abcObject &abc);

    Sample* newSample() override;
    void updateSample(const abcSampleSelector& ss) override;
    void readSample(Sample& sample, uint64_t idx) override;
    void cookSample(Sample& sample) override;

    void readSampleBody(Sample& sample, uint64_t idx);
    void cookSampleBody(Sample& sample);

private:
    aiAsyncLoad m_async_load;
};
