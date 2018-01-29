#pragma once

class aiCameraSample : public aiSampleBase
{
using super = aiSampleBase;
public:
    aiCameraSample(aiCamera *schema);

    void getData(aiCameraData &dst) const;

public:
    AbcGeom::CameraSample m_sample, m_next_sample;
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
    aiCamera(aiObject *obj);

    Sample* newSample() override;
    void readSample(Sample& sample, uint64_t idx) override;
};
