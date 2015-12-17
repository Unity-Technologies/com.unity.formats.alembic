#ifndef aeCamera_h
#define aeCamera_h

struct aeCameraSampleData
{
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
};

#endif // aeCamera_h
