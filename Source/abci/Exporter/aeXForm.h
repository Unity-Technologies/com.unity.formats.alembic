#pragma once

class aeXform : public aeSchema
{
    using super = aeSchema;
 public:
    aeXform(aeObject* parent, const char* name, uint32_t tsi);
    abcXform& getAbcObject() override;
    abcProperties getAbcProperties() override;

    size_t getNumSamples() override;
    void setFromPrevious() override;
    void writeSample(const aeXformData& data);

    void writeSampleBody();

 private:
    AbcGeom::OXformSchema m_schema;
    AbcGeom::XformSample m_sample;
    aeXformData m_data_local;
};
