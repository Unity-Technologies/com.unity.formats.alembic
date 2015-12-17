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

class aePoints : public aeSchemaBase
{
typedef aeSchemaBase super;
public:
    aePoints(aeObject *obj);
    void writeSample(const aePointsSampleData &data);

private:
    AbcGeom::OPoints m_abcobj;
    AbcGeom::OPointsSchema m_schema;
    AbcGeom::OPointsSchema::Sample m_sample;
};

#endif // aePoints_h
