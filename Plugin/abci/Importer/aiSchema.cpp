#include "pch.h"
#include "aiInternal.h"
#include "aiContext.h"
#include "aiObject.h"
#include "aiSchema.h"
#include "aiProperty.h"


aiSampleBase::aiSampleBase(aiSchemaBase *schema)
    : m_schema(schema)
{
}

aiSampleBase::~aiSampleBase()
{
}

const aiConfig & aiSampleBase::getConfig() const
{
    return m_schema->getConfig();
}


aiSchemaBase::aiSchemaBase(aiObject *obj)
    : m_obj(obj)
{
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
    return m_obj->getContext()->getConfig();
}

bool aiSchemaBase::isConstant() const { return m_constant; }
bool aiSchemaBase::isDataUpdated() const { return m_data_updated; }
void aiSchemaBase::markForceUpdate() { m_force_update = true; }

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
