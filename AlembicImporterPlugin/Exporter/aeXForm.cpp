#include "pch.h"
#include "AlembicExporter.h"
#include "aeObject.h"
#include "aeXForm.h"



aeXForm::aeXForm(aeObject *obj)
    : super(obj)
    , m_abcobj(obj->getAbcObject(), "XForm")
    , m_schema(m_abcobj.getSchema())
{

}

void aeXForm::writeSample(const aeXFormSampleData &data)
{
    m_sample.reset();

    AbcGeom::XformOp transop(AbcGeom::kTranslateOperation, AbcGeom::kTranslateHint);
    AbcGeom::XformOp scaleop(AbcGeom::kScaleOperation, AbcGeom::kScaleHint);
    AbcGeom::XformOp rotop(AbcGeom::kRotateOperation, AbcGeom::kRotateHint);
    m_sample.setInheritsXforms(data.inherits);
    m_sample.addOp(transop, data.translation);
    m_sample.addOp(rotop, data.rotation_axis, data.rotation_angle);
    m_sample.addOp(scaleop, data.scale);

    m_schema.set(m_sample);
}
