#ifndef aePoints_h
#define aePoints_h

class aePoints : public aeObject
{
typedef aeObject super;
public:
    aePoints(aeObject *parent, const char *name);
    AbcGeom::OPoints& getAbcObject() override;

    void writeSample(const aePointsSampleData &data);

private:
    AbcGeom::OPointsSchema m_schema;
};

#endif // aePoints_h
