#include "pch.h"
#include "AlembicExporter.h"
#include "aeObject.h"
#include "aeXForm.h"


aeXForm::aeXForm(aeObject *parent, const char *name)
    : super(parent->getContext(), parent, new AbcGeom::OXform(parent->getAbcObject(), name))
    , m_schema(getAbcObject().getSchema())
{
}

AbcGeom::OXform& aeXForm::getAbcObject()
{
    return dynamic_cast<AbcGeom::OXform&>(*m_abc);
}

void aeXForm::writeSample(const aeXFormSampleData &data)
{
    AbcGeom::XformOp transop(AbcGeom::kTranslateOperation, AbcGeom::kTranslateHint);
    AbcGeom::XformOp scaleop(AbcGeom::kScaleOperation, AbcGeom::kScaleHint);
    AbcGeom::XformOp rotop(AbcGeom::kRotateOperation, AbcGeom::kRotateHint);
    m_sample.setInheritsXforms(data.inherits);
    m_sample.addOp(transop, data.translation);
    m_sample.addOp(rotop, data.rotation_axis, data.rotation_angle);
    m_sample.addOp(scaleop, data.scale);

    m_schema.set(m_sample);
    m_sample.reset();
}
