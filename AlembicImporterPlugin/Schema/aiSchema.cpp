#include "pch.h"
#include "AlembicImporter.h"
#include "aiLogger.h"
#include "aiContext.h"
#include "aiObject.h"
#include "aiSchema.h"
#include "aiCamera.h"
#include "aiPolyMesh.h"
#include "aiXForm.h"


aiSampleBase::aiSampleBase(aiSchemaBase *schema)
    : m_schema(schema)
{
	m_config = schema->getConfig();
}

aiSampleBase::~aiSampleBase()
{
}


aiSchemaBase::aiSchemaBase(aiObject *obj)
    : m_obj(obj)
    , m_configCb(nullptr)
    , m_configCbArg(nullptr)
    , m_sampleCb(nullptr)
    , m_sampleCbArg(nullptr)
    , m_constant(false)
    , m_varyingTopology(false)
{
	// start with base config
	m_config = obj->getContext()->getConfig();
}

aiSchemaBase::~aiSchemaBase()
{
}

aiObject* aiSchemaBase::getObject()
{
    return m_obj;
}

const aiConfig& aiSchemaBase::getConfig() const
{
    return m_config;
}

void aiSchemaBase::setConfigCallback(aiConfigCallback cb, void *arg)
{
    m_configCb = cb;
    m_configCbArg = arg;
}

void aiSchemaBase::setSampleCallback(aiSampleCallback cb, void *arg)
{
    m_sampleCb = cb;
    m_sampleCbArg = arg;
}

void aiSchemaBase::invokeConfigCallback(aiConfig *config)
{
    if (m_configCb)
    {
    	m_configCb(m_configCbArg, config);
    }
}

void aiSchemaBase::invokeSampleCallback(aiSampleBase *sample, bool topologyChanged)
{
	if (m_sampleCb)
	{
		m_sampleCb(m_sampleCbArg, sample, topologyChanged);
	}
}

Abc::ISampleSelector aiSchemaBase::MakeSampleSelector(float time)
{
    return Abc::ISampleSelector(double(time), Abc::ISampleSelector::kFloorIndex);
}

Abc::ISampleSelector aiSchemaBase::MakeSampleSelector(int64_t index)
{
    return Abc::ISampleSelector(index, Abc::ISampleSelector::kFloorIndex);
}
