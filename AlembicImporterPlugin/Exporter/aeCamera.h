#ifndef aeCamera_h
#define aeCamera_h

struct aeCameraSampleData
{
};

class aeCamera : public aeSchemaBase
{
public:
    aeCamera(aeObject *obj);
    void writeSample(aeCameraSampleData &data);

private:
    AbcGeom::OCamera m_abcobj;
    AbcGeom::OCameraSchema m_schema;
};

#endif // aeCamera_h
