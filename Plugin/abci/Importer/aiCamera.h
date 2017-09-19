#pragma once

class aiCameraSample : public aiSampleBase
{
typedef aiSampleBase super;
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
    typedef aiCameraSample SampleT;
    typedef AbcGeom::ICameraSchema AbcSchemaT;
};


class aiCamera : public aiTSchema<aiCameraTraits>
{
typedef aiTSchema<aiCameraTraits> super;
public:
    aiCamera(aiObject *obj);

    Sample* newSample();
    Sample* readSample(const uint64_t idx, bool &topologyChanged) override;
};
