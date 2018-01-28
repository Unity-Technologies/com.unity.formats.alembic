#pragma once

class aiXformSample : public aiSampleBase
{
using super = aiSampleBase;
public:
    aiXformSample(aiXform *schema);

    void updateConfig(const aiConfig &config, bool &data_changed) override;

    void getData(aiXformData &dst) const;

public:
    AbcGeom::M44d m_matrix;
    AbcGeom::M44d m_next_matrix;
    bool inherits;
private:
    void decompose(const Imath::M44d &mat, Imath::V3d &scale, Imath::V3d &shear, Imath::Quatd &rotation, Imath::V3d &translation) const;
};


struct aiXformTraits
{
    using SampleT = aiXformSample;
    using AbcSchemaT = AbcGeom::IXformSchema;
};

class aiXform : public aiTSchema<aiXformTraits>
{
using super = aiTSchema<aiXformTraits>;
public:
    aiXform(aiObject *obj);

    Sample* newSample();
    Sample* readSample(const uint64_t idx) override;
};
