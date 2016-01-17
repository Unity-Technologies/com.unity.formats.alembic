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

abcProperties aeXForm::getAbcProperties()
{
    return m_schema.getUserProperties();
}

size_t aeXForm::getNumSamples()
{
    return m_schema.getNumSamples();
}

void aeXForm::setFromPrevious()
{
    m_schema.setFromPrevious();
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
    data.translation *= getConfig().scale;

    m_sample.setInheritsXforms(data.inherits);
    if (getConfig().xformType == aeXFromType_Matrix)
    {
        const float Deg2Rad = float(M_PI) / 180.0f;
        abcM44 trans;
        trans *= abcM44().setScale(data.scale);
        trans *= abcM44().setAxisAngle(data.rotationAxis, data.rotationAngle*Deg2Rad);
        trans *= abcM44().setTranslation(data.translation);
        m_sample.setMatrix(abcM44d(trans));
    }
    else if (getConfig().xformType == aeXFromType_TRS)
    {
        m_sample.setTranslation(data.translation);
        m_sample.setRotation(data.rotationAxis, data.rotationAngle);
        m_sample.setScale(data.scale);
    }

    m_schema.set(m_sample);
    m_sample.reset();
}
