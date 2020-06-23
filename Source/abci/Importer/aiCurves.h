#pragma once
#include "pch.h"
#include "aiInternal.h"
#include "aiContext.h"
#include "aiObject.h"
#include "aiSchema.h"

class aiCurves;

class aiCurvesSample : public aiSample
{
    using super = aiSample;
public:
    aiCurvesSample(aiCurves *schema);
    void getSummary(aiCurvesSampleSummary &dst);

    ~aiCurvesSample(){}
    Abc::P3fArraySamplePtr m_position_sp;
    RawVector<abcV3> m_positions;

    Abc::Int32ArraySamplePtr m_numVertices_sp;
    RawVector<int32_t> m_numVertices;

    AbcGeom::ITypedGeomParam<Abc::V2fTPTraits>::sample_type m_uvs_sp;
    RawVector<abcV2> m_uvs;
    RawVector<int> m_remap_uvs;

    AbcGeom::ITypedGeomParam<Abc::Float32TPTraits>::sample_type m_widths_sp;
    RawVector<float> m_widths;
    RawVector<int> m_remap_widths;

    void fillData(aiCurvesData& data);
};

struct aiCurvesTraits
{
    using SampleT = aiCurvesSample;
    using AbcSchemaT = AbcGeom::ICurvesSchema;
};

class aiCurves : public aiTSchema<aiCurvesTraits>
{
    using super = aiTSchema<aiCurvesTraits>;
public:
    aiCurves(aiObject *parent, const abcObject &abc);
    ~aiCurves() override {};

    Sample* newSample() override;
    void readSampleBody(Sample& sample, uint64_t idx) override;
    void cookSampleBody(Sample& sample) override;
    const aiCurvesSummary& getSummary() const {return m_summary;}
private:
    void updateSummary();
    aiCurvesSummary m_summary;
};
