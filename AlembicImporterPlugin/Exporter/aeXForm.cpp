#include "pch.h"
#include "AlembicExporter.h"
#include "aeContext.h"
#include "aeObject.h"
#include "aeXForm.h"


aeXForm::aeXForm(aeObject *parent, const char *name)
    : super(parent->getContext(), parent, new abcXForm(parent->getAbcObject(), name, parent->getContext()->getTimeSaplingIndex()))
    , m_schema(getAbcObject().getSchema())
{
}

abcXForm& aeXForm::getAbcObject()
{
    return dynamic_cast<abcXForm&>(*m_abc);
}

abcProperties* aeXForm::getAbcProperties()
{
    return &m_schema;
}

void aeXForm::writeSample(const aeXFormSampleData &data_)
{
    auto data = data_;
    if (getConfig().swapHandedness)
    {
        data.translation.x *= -1.0f;
        data.rotationAxis.x *= -1.0f;
        data.rotationAngle *= -1.0f;
    }

    // single kMatrixOperation is best for compatibility

    const float Deg2Rad = M_PI / 180.0f;
    abcM44 trans;
    trans *= abcM44().setScale(data.scale);
    trans *= abcM44().setAxisAngle(data.rotationAxis, data.rotationAngle*Deg2Rad);
    trans *= abcM44().setTranslation(data.translation);

    AbcGeom::XformOp matop(AbcGeom::kMatrixOperation, AbcGeom::kMatrixHint);
    AbcGeom::XformSample sample;
    sample.setInheritsXforms(data.inherits);
    sample.addOp(matop, abcM44d(trans));
    m_schema.set(sample);
}
