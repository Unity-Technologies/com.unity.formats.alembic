#pragma once

class aeXform : public aeObject
{
using super = aeObject;
public:
    aeXform(aeObject *parent, const char *name, uint32_t tsi);
    abcXform& getAbcObject() override;
    abcProperties getAbcProperties() override;

    size_t  getNumSamples() override;
    void    setFromPrevious() override;
    void    writeSample(const aeXformData &data);

private:
    AbcGeom::OXformSchema m_schema;
    AbcGeom::XformSample m_sample;
};
