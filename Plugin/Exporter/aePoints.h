#ifndef aePoints_h
#define aePoints_h

class aePoints : public aeObject
{
typedef aeObject super;
public:
    aePoints(aeObject *parent, const char *name);
    abcPoints& getAbcObject() override;
    abcProperties getAbcProperties() override;

    void writeSample(const aePointsSampleData &data);
    void setFromPrevious() override;

private:
    AbcGeom::OPointsSchema m_schema;

    std::vector<uint64_t> m_buf_ids;
    std::vector<abcV3> m_buf_positions;
    std::vector<abcV3> m_buf_velocities;
};

#endif // aePoints_h
