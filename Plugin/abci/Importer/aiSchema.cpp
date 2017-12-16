#include "pch.h"
#include "aiInternal.h"
#include "aiContext.h"
#include "aiObject.h"
#include "aiSchema.h"
#include "aiProperty.h"


aiSampleBase::aiSampleBase(aiSchemaBase *schema)
    : m_currentTimeOffset(0)
    , m_currentTimeInterval(0)
    , m_schema(schema)
{
    m_config = schema->getConfig();
}

aiSampleBase::~aiSampleBase()
{
}


aiSchemaBase::aiSchemaBase(aiObject *obj)
    : m_obj(obj)
{
    // start with base config
    m_config = obj->getContext()->getConfig();
}

aiSchemaBase::~aiSchemaBase()
{
    m_properties.clear();
}

aiObject* aiSchemaBase::getObject() const
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

void aiSchemaBase::invokeConfigCallback(aiConfig *config) const
{
    if (m_configCb)
    {
        m_configCb(m_configCbArg, config);
    }
}

void aiSchemaBase::invokeSampleCallback(aiSampleBase *sample, bool topologyChanged) const
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

    DebugLog("  Original config: %s", ToString(m_config).c_str());

    // get object config overrides (if any)
    invokeConfigCallback(&m_config);

    DebugLog("  Override config: %s", ToString(m_config).c_str());
}

int aiSchemaBase::getNumProperties() const
{
    return static_cast<int>(m_properties.size());
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
        [](const aiPropertyPtr& a, const std::string& name) { return a->getName() < name; });
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
        auto *prop = aiMakeProperty(this, cpro, header);
        if (prop != nullptr) {
            m_properties.emplace_back(prop);
        }
    }
    std::sort(m_properties.begin(), m_properties.end(),
        [](const aiPropertyPtr& a, const aiPropertyPtr& b) { return a->getName() < b->getName(); });
}

void aiSchemaBase::updateProperties(const abcSampleSelector& ss)
{
    for (auto &prop : m_properties) {
        prop->updateSample(ss);
    }
}
