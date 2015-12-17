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

class aeCamera : public aeObject
{
typedef aeObject super;
public:
    aeCamera(aeObject *parent, const char *name);
    AbcGeom::OCamera& getAbcObject() override;

    void writeSample(const aeCameraSampleData &data);

private:
    AbcGeom::OCameraSchema m_schema;
};

#endif // aeCamera_h
