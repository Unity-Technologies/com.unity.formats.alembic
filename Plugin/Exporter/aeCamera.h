#ifndef aeCamera_h
#define aeCamera_h

class aeCamera : public aeObject
{
typedef aeObject super;
public:
    aeCamera(aeObject *parent, const char *name);
    abcCamera& getAbcObject() override;
    abcProperties getAbcProperties() override;

    void writeSample(const aeCameraSampleData &data);
    void setFromPrevious() override;

private:
    AbcGeom::OCameraSchema m_schema;
};

#endif // aeCamera_h
