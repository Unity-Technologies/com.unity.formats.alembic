#ifndef aeCamera_h
#define aeCamera_h

struct aeCameraSampleData
{
    float nearClippingPlane;
    float farClippingPlane;
    float fieldOfView;
    float focusDistance;
    float focalLength;

    inline aeCameraSampleData()
        : nearClippingPlane(0.0f)
        , farClippingPlane(0.0f)
        , fieldOfView(0.0f)
        , focusDistance(0.0f)
        , focalLength(0.0f)
    {
    }
};

class aeCamera : public aeSchemaBase
{
typedef aeSchemaBase super;
public:
    aeCamera(aeObject *obj);
    void writeSample(const aeCameraSampleData &data);

private:
    AbcGeom::OCamera m_abcobj;
    AbcGeom::OCameraSchema m_schema;
    AbcGeom::CameraSample m_sample;
};

#endif // aeCamera_h
