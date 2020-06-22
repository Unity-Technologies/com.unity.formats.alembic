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

    ~aiCurvesSample(){}
    // void fillData(aiPointsData &dst);
    // void getSummary(aiPointsSampleSummary &dst);
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
};
