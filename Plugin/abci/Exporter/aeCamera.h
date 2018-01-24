#pragma once

class aeCamera : public aeObject
{
using super = aeObject;
public:
    aeCamera(aeObject *parent, const char *name, uint32_t tsi);
    abcCamera& getAbcObject() override;
    abcProperties getAbcProperties() override;

    size_t  getNumSamples() override;
    void    setFromPrevious() override;
    void    writeSample(const aeCameraData &data);

private:
    AbcGeom::OCameraSchema m_schema;
};
