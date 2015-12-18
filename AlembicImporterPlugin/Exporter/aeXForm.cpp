#include "pch.h"
#include "AlembicExporter.h"
#include "aeContext.h"
#include "aeObject.h"
#include "aeXForm.h"


aeXForm::aeXForm(aeObject *parent, const char *name)
    : super(parent->getContext(), parent, new AbcGeom::OXform(parent->getAbcObject(), name, parent->getContext()->getTimeSaplingIndex()))
    , m_schema(getAbcObject().getSchema())
{
}

AbcGeom::OXform& aeXForm::getAbcObject()
{
    return dynamic_cast<AbcGeom::OXform&>(*m_abc);
}

void aeXForm::writeSample(const aeXFormSampleData &data_)
{
    auto data = data_;
    if (getConfig().swapHandedness)
    {
        data.translation.x *= -1.0f;
        data.rotation_axis.x *= -1.0f;
        data.rotation_angle *= -1.0f;
    }

    AbcGeom::XformOp transop(AbcGeom::kTranslateOperation, AbcGeom::kTranslateHint);
    AbcGeom::XformOp scaleop(AbcGeom::kScaleOperation, AbcGeom::kScaleHint);
    AbcGeom::XformOp rotop(AbcGeom::kRotateOperation, AbcGeom::kRotateHint);
    AbcGeom::XformSample sample;
    sample.setInheritsXforms(data.inherits);
    sample.addOp(transop, data.translation);
    sample.addOp(rotop, data.rotation_axis, data.rotation_angle);
    sample.addOp(scaleop, data.scale);

    m_schema.set(sample);
}
