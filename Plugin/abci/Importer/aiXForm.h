#pragma once
#include "aiAsync.h"

class aiXformSample : public aiSample
{
using super = aiSample;
public:
    aiXformSample(aiXform *schema);

    void getData(aiXformData &dst) const;

public:
    AbcGeom::M44d m_matrix, m_next_matrix;
    bool inherits;
    aiXformData m_data;
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
    aiXform(aiObject *parent, const abcObject &abc);

    Sample* newSample() override;
    void updateSample(const abcSampleSelector& ss) override;
    void readSample(Sample& sample, uint64_t idx) override;
    void cookSample(Sample& sample) override;

    void readSampleBody(Sample& sample, uint64_t idx);
    void cookSampleBody(Sample& sample);
    void decompose(const Imath::M44d &mat, Imath::V3d &scale, Imath::V3d &shear, Imath::Quatd &rotation, Imath::V3d &translation) const;

private:
    aiAsyncLoad m_async_load;
};
