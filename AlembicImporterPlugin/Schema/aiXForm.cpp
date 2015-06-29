#include "pch.h"
#include "AlembicImporter.h"
#include "aiSchema.h"
#include "aiObject.h"
#include "aiContext.h"
#include "aiXForm.h"

#define aiPI 3.14159265f


aiXFormSample::aiXFormSample(aiXForm *xf, float time)
    : super(xf, time)
{
}

void aiXFormSample::getData(aiXFormData &o_data) const
{
    o_data.inherits = m_sample.getInheritsXforms();
    o_data.translation = m_sample.getTranslation();
    o_data.scale = abcV3(m_sample.getScale());

    abcV3 axis = m_sample.getAxis();
    float angle = m_sample.getAngle() * (aiPI / 180.0f);

    if (m_schema->getImportConfig().revert_x) {
        o_data.translation.x *= -1.0f;
        axis.x *= -1.0f;
        angle *= -1.0f;
    }

    o_data.rotation = abcV4(
        axis.x * std::sin(angle * 0.5f),
        axis.y * std::sin(angle * 0.5f),
        axis.z * std::sin(angle * 0.5f),
        std::cos(angle * 0.5f) );
}



aiXForm::aiXForm(aiObject *obj)
    : super(obj)
{
    AbcGeom::IXform xf(obj->getAbcObject(), Abc::kWrapExisting);
    m_schema = xf.getSchema();
}

aiXForm::Sample* aiXForm::readSample(float time)
{
    Sample *ret = new Sample(this, time);
    m_schema.get(ret->m_sample, makeSampleSelector(time));
    return ret;
}



