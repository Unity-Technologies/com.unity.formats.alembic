#ifndef aiCamera_h
#define aiCamera_h


struct aiCameraData
{
    float near_clipping_plane;
    float far_clipping_plane;
    float field_of_view;
    float focus_distance;
    float focal_length;
};



class aiCameraSample : public aiSampleBase
{
typedef aiSampleBase super;
public:
    aiCameraSample(aiCamera *schema, aiIndex index);
    void getParams(aiCameraData &o_params);

public:
    AbcGeom::CameraSample m_sample;
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
    Sample* readSample(float time) override;
    void debugDump() const override;

private:
    AbcGeom::ICameraSchema m_schema;
};

#endif // aiCamera_h
