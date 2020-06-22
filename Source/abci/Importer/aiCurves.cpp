#include "aiCurves.h"

aiCurvesSample::aiCurvesSample(aiCurves *schema) : super(schema)
{
}

aiCurves::aiCurves(aiObject *parent, const abcObject &abc) : super(parent, abc)
{

}

aiCurvesSample *aiCurves::newSample()
{
    return new Sample(this);
}

void aiCurves::readSampleBody(aiCurvesSample &sample, uint64_t idx)
{

}

void aiCurves::cookSampleBody(aiCurvesSample &sample)
{

}
