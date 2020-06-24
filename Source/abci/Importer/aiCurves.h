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
    Abc::P3fArraySamplePtr m_position_sp,m_position_sp2;
    RawVector<abcV3> m_positions, m_positions2;

    Abc::Int32ArraySamplePtr m_numVertices_sp, m_numVertices_sp2;
    RawVector<int32_t> m_numVertices, m_numVertices2;

    AbcGeom::ITypedGeomParam<Abc::V2fTPTraits>::sample_type m_uvs_sp, m_uvs_sp2;
    RawVector<abcV2> m_uvs, m_uvs2;

    AbcGeom::ITypedGeomParam<Abc::Float32TPTraits>::sample_type m_widths_sp, m_widths_sp2;

    RawVector<float> m_widths, m_widths2;

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
