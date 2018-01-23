#pragma once

class aePoints : public aeObject
{
typedef aeObject super;
public:
    aePoints(aeObject *parent, const char *name, uint32_t tsi);
    abcPoints& getAbcObject() override;
    abcProperties getAbcProperties() override;

    size_t  getNumSamples() const;
    void    setFromPrevious() override;
    void    writeSample(const aePointsData &data);

private:
    void    doWriteSample();

    AbcGeom::OPointsSchema m_schema;
    RawVector<uint64_t> m_buf_ids;
    RawVector<abcV3> m_buf_positions;
    RawVector<abcV3> m_buf_velocities;
};
