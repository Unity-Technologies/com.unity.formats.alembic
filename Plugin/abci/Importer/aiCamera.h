#pragma once

class aiCameraSample : public aiSampleBase
{
using super = aiSampleBase;
public:
    aiCameraSample(aiCamera *schema);
    
    void updateConfig(const aiConfig &config, bool &topoChanged, bool &dataChanged) override;

    void getData(aiCameraData &outData) const;
    
public:
    AbcGeom::CameraSample m_sample;
    AbcGeom::CameraSample m_nextSample;
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

    Sample* newSample();
    Sample* readSample(const uint64_t idx, bool &topologyChanged) override;
};
