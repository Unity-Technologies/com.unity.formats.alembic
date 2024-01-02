#pragma once
#include "pch.h"
#include "aiInternal.h"
#include "aiContext.h"
#include "aiObject.h"
#include "aiSchema.h"

class aiCurves;

struct aiCurvesSummaryInternal : aiCurvesSummary
{
    bool has_velocity; 
    std::vector<bool>* has_attributes = new std::vector<bool>(false);
    std::vector<bool>* constant_attribute = new std::vector<bool>(false);
    std::vector<bool>* has_attributes_prop = new std::vector<bool>(false);
    std::vector<bool>* interpolate_attributes = new std::vector<bool>(false);
};

class aiCurvesSample : public aiSample
{
    using super = aiSample;
public:
    aiCurvesSample(aiCurves *schema);
    void getSummary(aiCurvesSampleSummary &dst);

    ~aiCurvesSample() {}
    Abc::P3fArraySamplePtr m_position_sp, m_position_sp2;
    RawVector<abcV3> m_positions, m_positions2, m_positions_prev;

    Abc::Int32ArraySamplePtr m_numVertices_sp;
    RawVector<int32_t> m_numVertices;

    AbcGeom::ITypedGeomParam<Abc::V2fTPTraits>::sample_type m_uvs_sp, m_uvs_sp2;
    RawVector<abcV2> m_uvs, m_uvs2;

    AbcGeom::ITypedGeomParam<Abc::Float32TPTraits>::sample_type m_widths_sp, m_widths_sp2;

    RawVector<float> m_widths, m_widths2;

    Abc::V3fArraySamplePtr m_velocities_sp;
    RawVector<abcV3> m_velocities;

    std::vector<AttributeData*>* m_attributes_ref = new std::vector<AttributeData*>();

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
    ~aiCurves() override {}

    Sample* newSample() override;
    void readSampleBody(Sample& sample, uint64_t idx) override;
    void cookSampleBody(Sample& sample) override;
    template < typename Tp >
    void ReadAttribute(aiObject* object, std::vector<AttributeData*>* attributes);
    const aiCurvesSummaryInternal& getSummary() const {return m_summary;}
    std::vector<AttributeData*>* m_attributes_param = new std::vector<AttributeData*>();
    bool m_varying_topology = false;

private:
    void updateSummary();
    aiCurvesSummaryInternal m_summary;
};

