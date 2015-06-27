#include "pch.h"
#include "AlembicImporter.h"
#include "aiSchema.h"
#include "aiObject.h"
#include "aiContext.h"


aiSampleBase::aiSampleBase(aiSchemaBase *schema, float time)
    : m_schema(schema)
    , m_time(time)
{
}

aiSampleBase::~aiSampleBase()
{
}


aiSchemaBase::aiSchemaBase(aiObject *obj)
    : m_obj(obj)
    , m_cb(nullptr)
    , m_cb_arg(nullptr)
{}
aiSchemaBase::~aiSchemaBase() {}

aiObject* aiSchemaBase::getObject()
{
    return m_obj;
}

const aiImportConfig& aiSchemaBase::getImportConfig() const
{
    return m_obj->getContext()->getImportConfig();
}

void aiSchemaBase::setCallback(aiSampleCallback cb, void *arg)
{
    m_cb = cb;
    m_cb_arg = arg;
}


void aiSchemaBase::invokeCallback(aiSampleBase *sample)
{
    if (m_cb) {
        m_cb(m_cb_arg, sample);
    }
}

Abc::ISampleSelector aiSchemaBase::makeSampleSelector(float time)
{
    return Abc::ISampleSelector(time, Abc::ISampleSelector::kFloorIndex);
}

Abc::ISampleSelector aiSchemaBase::makeSampleSelector(uint32_t index)
{
    return Abc::ISampleSelector(index, Abc::ISampleSelector::kFloorIndex);
}
