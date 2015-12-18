#include "pch.h"
#include "AlembicExporter.h"
#include "aeContext.h"
#include "aeObject.h"
#include "aePoints.h"


aePoints::aePoints(aeObject *parent, const char *name)
    : super(parent->getContext(), parent, new AbcGeom::OPoints(parent->getAbcObject(), name, parent->getContext()->getTimeSaplingIndex()))
    , m_schema(getAbcObject().getSchema())
{
}

AbcGeom::OPoints& aePoints::getAbcObject()
{
    return dynamic_cast<AbcGeom::OPoints&>(*m_abc);
}

void aePoints::writeSample(const aePointsSampleData &data)
{
    AbcGeom::OPointsSchema::Sample sample;
    sample.setPositions(Abc::P3fArraySample(data.positions, data.count));

    m_schema.set(sample);
}
