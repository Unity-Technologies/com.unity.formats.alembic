#pragma once

class aeCamera : public aeSchema
{
using super = aeSchema;
public:
    aeCamera(aeObject *parent, const char *name, uint32_t tsi);
    abcCamera& getAbcObject() override;
    abcProperties getAbcProperties() override;

    size_t  getNumSamples() override;
    void    setFromPrevious() override;
    void    writeSample(const CameraData &data);

    void    writeSampleBody();

private:
    AbcGeom::OCameraSchema m_schema;
    CameraData m_data_local;
};
