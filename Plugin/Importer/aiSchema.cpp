#include "pch.h"
#include "AlembicImporter.h"
#include "aiLogger.h"
#include "aiContext.h"
#include "aiObject.h"
#include "aiSchema.h"
#include "aiProperty.h"


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
    , m_pendingSample(0)
    , m_pendingTopologyChanged(false)
{
	// start with base config
	m_config = obj->getContext()->getConfig();
}

aiSchemaBase::~aiSchemaBase()
{
    m_properties.clear();
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

void aiSchemaBase::readConfig()
{
    DebugLog("aiSchemaBase::readConfig");

    m_config = m_obj->getContext()->getConfig();

    bool useThreads = m_config.useThreads;

    DebugLog("  Original config: %s", m_config.toString().c_str());

    // get object config overrides (if any)
    invokeConfigCallback(&m_config);

    // don't allow override of useThreads option
    m_config.useThreads = useThreads;

    DebugLog("  Override config: %s", m_config.toString().c_str());
}

void aiSchemaBase::notifyUpdate()
{
    if (m_pendingSample)
    {
        invokeSampleCallback(m_pendingSample, m_pendingTopologyChanged);

        m_pendingSample = 0;
        m_pendingTopologyChanged = false;
    }
}


int aiSchemaBase::getNumProperties() const
{
    return (int)m_properties.size();
}

aiProperty* aiSchemaBase::getPropertyByIndex(int i)
{
    auto& r = m_properties[i];
    if (r != nullptr) { r->setActive(true); }
    return r.get();
}

aiProperty* aiSchemaBase::getPropertyByName(const std::string& name)
{
    auto i = std::lower_bound(m_properties.begin(), m_properties.end(), name,
        [](aiPropertyPtr& a, const std::string& name) { return a->getName() < name; });
    if (i != m_properties.end()) {
        (*i)->setActive(true);
        return i->get();
    }
    return nullptr;
}

void aiSchemaBase::setupProperties()
{
    auto cpro = getAbcProperties();
    if (!cpro.valid()) { return; }

    size_t n = cpro.getNumProperties();
    for (size_t i = 0; i < n; ++i) {
        auto header = cpro.getPropertyHeader(i);
        auto *prop = aiMakeProperty(cpro, header);
        if (prop != nullptr) {
            m_properties.emplace_back(prop);
        }
    }
    std::sort(m_properties.begin(), m_properties.end(),
        [](aiPropertyPtr& a, aiPropertyPtr& b) { return a->getName() < b->getName(); });
}

void aiSchemaBase::updateProperties(const abcSampleSelector& ss)
{
    for (auto &prop : m_properties) {
        prop->updateSample(ss);
    }
}
