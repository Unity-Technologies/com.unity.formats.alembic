#ifndef aiCamera_h
#define aiCamera_h

struct aiCameraParams
{
    float targetAspect;
    float nearClippingPlane;
    float farClippingPlane;
    float fieldOfView;
    float focusDistance;
    float focalLength;
};

class aiCamera : public aiSchema
{
typedef aiSchema super;
public:
    aiCamera();
    aiCamera(aiObject *obj);
    virtual ~aiCamera();

    void updateSample() override;

    void getParams(aiCameraParams &params);

private:
    AbcGeom::ICameraSchema m_schema;
    AbcGeom::CameraSample m_sample;
};

#endif
