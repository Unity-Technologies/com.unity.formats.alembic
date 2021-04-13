#pragma once

class aiXformSample : public aiSample
{
    using super = aiSample;
 public:
    aiXformSample(aiXform* schema);

    void getData(aiXformData& dst) const;

 public:
    AbcGeom::XformSample xf_sp, xf_sp2;
    aiXformData data;
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
    aiXform(aiObject* parent, const abcObject& abc);

    Sample* newSample() override;
    void readSampleBody(Sample& sample, uint64_t idx) override;
    void cookSampleBody(Sample& sample) override;
    void decompose(const Imath::M44d& mat,
        Imath::V3d& scale,
        Imath::V3d& shear,
        Imath::Quatd& rotation,
        Imath::V3d& translation) const;
};
