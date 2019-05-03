#pragma once
#include <../Foundation/AbcNodes/CameraData.h>
class aiCameraSample : public aiSample
{
using super = aiSample;
public:
    aiCameraSample(aiCamera *schema);

    void getData(CameraData &dst) const;

public:
    AbcGeom::CameraSample cam_sp, cam_sp2;
    CameraData data;
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
    void readSampleBody(Sample& sample, uint64_t idx) override;
    void cookSampleBody(Sample& sample) override;
};
