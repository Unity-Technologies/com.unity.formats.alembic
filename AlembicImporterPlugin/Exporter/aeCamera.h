#ifndef aeCamera_h
#define aeCamera_h

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
