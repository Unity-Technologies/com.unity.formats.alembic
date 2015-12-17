#ifndef aePoints_h
#define aePoints_h


struct aePointsSampleData
{
    abcV3 *positions;
    int count;

    inline aePointsSampleData()
        : positions(nullptr)
        , count(0)
    {
    }
};

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
