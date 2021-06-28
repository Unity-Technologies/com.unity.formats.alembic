#pragma once
#include<Foundation/Vector.h>
class aePoints : public aeSchema
{
    using super = aeSchema;
public:
    aePoints(aeObject *parent, const char *name, uint32_t tsi);
    abcPoints& getAbcObject() override;
    abcProperties getAbcProperties() override;

    size_t  getNumSamples() override;
    void    setFromPrevious() override;
    void    writeSample(const aePointsData &data);

private:
    void    doWriteSample();

    AbcGeom::OPointsSchema m_schema;
    bool m_buf_visibility = true;
    Vector<uint64_t> m_buf_ids;
    Vector<abcV3> m_buf_positions;
    Vector<abcV3> m_buf_velocities;
};
