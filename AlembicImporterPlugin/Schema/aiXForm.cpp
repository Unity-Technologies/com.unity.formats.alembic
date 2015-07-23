#include "pch.h"
#include "AlembicImporter.h"
#include "aiSchema.h"
#include "aiCamera.h"
#include "aiPolyMesh.h"
#include "aiXForm.h"
#include "aiObject.h"
#include "aiContext.h"

aiXForm::aiXForm()
    : super()
    , m_inherits(true)
{
}

aiXForm::aiXForm(aiObject *obj)
    : super(obj)
    , m_inherits(true)
{
}

aiXForm::~aiXForm()
{
}

void aiXForm::updateSample()
{
    Abc::ISampleSelector ss(m_obj->getCurrentTime());

    if (!m_schema.valid())
    {
        AbcGeom::IXform xf(m_obj->getAbcObject(), Abc::kWrapExisting);
        m_schema = xf.getSchema();
    }
    else if (m_schema.isConstant())
    {
        return;
    }

    m_schema.get(m_sample, ss);
    m_inherits = m_schema.getInheritsXforms(ss);
}

bool aiXForm::getInherits() const
{
    return m_inherits;
}

abcV3 aiXForm::getPosition() const
{
    abcV3 ret = m_sample.getTranslation();
    if (m_obj->isHandednessSwapped())
    {
        ret.x *= -1.0f;
    }
    return ret;
}

abcV3 aiXForm::getAxis() const
{
    abcV3 ret = m_sample.getAxis();
    if (m_obj->isHandednessSwapped())
    {
        ret.x *= -1.0f;
    }
    return ret;
}

float aiXForm::getAngle() const
{
    float ret = float(m_sample.getAngle());
    if (m_obj->isHandednessSwapped())
    {
        ret *= -1.0f;
    }
    return ret;
}

abcV3 aiXForm::getScale() const
{
    return abcV3(m_sample.getScale());
}

abcM44 aiXForm::getMatrix() const
{
    return abcM44(m_sample.getMatrix());
}
