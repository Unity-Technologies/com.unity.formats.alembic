#include "pch.h"
#include "aeInternal.h"
#include "aeContext.h"
#include "aeObject.h"
#include "aeXForm.h"

aeXform::aeXform(aeObject* parent, const char* name, uint32_t tsi)
    : super(parent->getContext(), parent, new abcXform(parent->getAbcObject(), name, tsi), tsi),
      m_schema(getAbcObject().getSchema())
{
}

abcXform& aeXform::getAbcObject()
{
    return dynamic_cast<abcXform&>(*m_abc);
}

abcProperties aeXform::getAbcProperties()
{
    return m_schema.getUserProperties();
}

size_t aeXform::getNumSamples()
{
    return m_schema.getNumSamples();
}

void aeXform::setFromPrevious()
{
    m_schema.setFromPrevious();
}

void aeXform::writeSample(const aeXformData& data)
{
    m_data_local = data;
    m_ctx->addAsync([this]()
    { writeSampleBody(); });
}

void aeXform::writeSampleBody()
{
    auto& data = m_data_local;

    writeVisibility(data.visibility);

    // quaternion to angle axis
    auto& quat = data.rotation;

    float angle = 0.0f;
    abcV3 axis = abcV3(0.0f, 1.0f, 0.0f);
    if (abs(quat.w) != 1.0f)
    {
        float qw2 = quat.w * quat.w;
        angle = std::acos(quat.w) * 2.0f;
        axis = abcV3(
            quat.x / std::sqrt(1.0f - qw2),
            quat.y / std::sqrt(1.0f - qw2),
            quat.z / std::sqrt(1.0f - qw2));
    }

    if (getConfig().swap_handedness)
    {
        data.translation.x *= -1.0f;
        axis.x *= -1.0f;
        angle *= -1.0f;
    }
    data.translation *= getConfig().scale_factor;

    m_sample.setInheritsXforms(data.inherits);
    if (getConfig().xform_type == aeXFromType::Matrix)
    {
        abcM44 trans;
        trans *= abcM44().setScale(data.scale);
        trans *= abcM44().setAxisAngle(axis, angle);
        trans *= abcM44().setTranslation(data.translation);
        m_sample.setMatrix(abcM44d(trans));
    }
    else if (getConfig().xform_type == aeXFromType::TRS)
    {
        const float Rad2Deg = 1.0f / (float(M_PI) / 180.0f);
        m_sample.setTranslation(data.translation);
        m_sample.setRotation(axis, angle * Rad2Deg);
        m_sample.setScale(data.scale);
    }

    m_schema.set(m_sample);
    m_sample.reset();
}
