#include "pch.h"
#include "aeInternal.h"
#include "aeContext.h"
#include "aeObject.h"
#include "aeXForm.h"
#include "aePoints.h"
#include "aePolyMesh.h"
#include "aeCamera.h"

aeProperty::aeProperty()
{
}

aeProperty::~aeProperty()
{
}

// array property

template<class T>
class aeTArrayProprty : public aeProperty
{
    using super = aeProperty;
 public:
    using property_type = T;
    using value_type = typename T::value_type;
    using sample_type = typename T::sample_type;

    aeTArrayProprty(aeSchema* parent, const char* name, uint32_t tsi)
        : m_abcprop(new property_type(parent->getAbcProperties(), name, tsi))
    {
        DebugLog("aeTArrayProprty::aeTArrayProprty() %s", m_abcprop->getName().c_str());
    }

    const char* getName() const override
    {
        return m_abcprop->getName().c_str();
    }
    bool isArray() const override
    {
        return true;
    }

    void writeSample(const void* data, int data_num) override
    {
        m_abcprop->set(sample_type((const value_type*)data, data_num));
    }

 private:
    std::unique_ptr<property_type> m_abcprop;
};
template
class aeTArrayProprty<abcBoolArrayProperty>;
template
class aeTArrayProprty<abcIntArrayProperty>;
template
class aeTArrayProprty<abcUIntArrayProperty>;
template
class aeTArrayProprty<abcFloatArrayProperty>;
template
class aeTArrayProprty<abcFloat2ArrayProperty>;
template
class aeTArrayProprty<abcFloat3ArrayProperty>;
template
class aeTArrayProprty<abcFloat4ArrayProperty>;
template
class aeTArrayProprty<abcFloat4x4ArrayProperty>;


// scalar property

template<class T>
class aeTScalarProprty : public aeProperty
{
    using super = aeProperty;
 public:
    using property_type = T;
    using value_type = typename T::value_type;

    aeTScalarProprty(aeSchema* parent, const char* name, uint32_t tsi)
        : m_abcprop(new property_type(parent->getAbcProperties(), name, tsi))
    {
        DebugLog("aeTScalarProprty::aeTScalarProprty() %s", m_abcprop->getName().c_str());
    }

    const char* getName() const override
    {
        return m_abcprop->getName().c_str();
    }
    bool isArray() const override
    {
        return false;
    }

    void writeSample(const void* data, int /*data_num*/) override
    {
        m_abcprop->set(*(const value_type*)data);
    }

 private:
    std::unique_ptr<property_type> m_abcprop;
};
template
class aeTScalarProprty<abcBoolProperty>;
template
class aeTScalarProprty<abcIntProperty>;
template
class aeTScalarProprty<abcUIntProperty>;
template
class aeTScalarProprty<abcFloatProperty>;
template
class aeTScalarProprty<abcFloat2Property>;
template
class aeTScalarProprty<abcFloat3Property>;
template
class aeTScalarProprty<abcFloat4Property>;
template
class aeTScalarProprty<abcFloat4x4Property>;

template<class T, bool is_array = std::is_base_of<Abc::OArrayProperty, T>::value>
struct aeMakeProperty;

template<class T>
struct aeMakeProperty<T, false>
{
    static aeProperty* make(aeSchema* parent, const char* name, uint32_t tsi)
    {
        return new aeTScalarProprty<T>(parent, name, tsi);
    }
};

template<class T>
struct aeMakeProperty<T, true>
{
    static aeProperty* make(aeSchema* parent, const char* name, uint32_t tsi)
    {
        return new aeTArrayProprty<T>(parent, name, tsi);
    }
};

aeObject::aeObject(aeContext* ctx, aeObject* parent, abcObject* abc, uint32_t tsi)
    : m_ctx(ctx), m_parent(parent), m_abc(abc), m_tsi(tsi)
{
}

aeObject::~aeObject()
{
    if (!m_children.empty())
    {
        // make m_children empty before deleting children because children try to remove element of it in their destructor
        decltype(m_children) tmp;
        tmp.swap(m_children);
    }
    if (m_parent)
        m_parent->removeChild(this);
}

const char* aeObject::getName() const
{
    return m_abc->getName().c_str();
}
const char* aeObject::getFullName() const
{
    return m_abc->getFullName().c_str();
}
uint32_t aeObject::getTimeSamplingIndex() const
{
    return m_tsi;
}
size_t aeObject::getNumChildren() const
{
    return m_children.size();
}
aeObject* aeObject::getChild(int i)
{
    return m_children[i].get();
}
aeObject* aeObject::getParent()
{
    return m_parent;
}

aeContext* aeObject::getContext()
{
    return m_ctx;
}
const aeConfig& aeObject::getConfig() const
{
    return m_ctx->getConfig();
}
AbcGeom::OObject& aeObject::getAbcObject()
{
    return *m_abc;
}

template<class T>
T* aeObject::newChild(const char* name, uint32_t tsi)
{
    T* child = new T(this, name, tsi == 0 ? getTimeSamplingIndex() : tsi);
    m_children.emplace_back(child);
    return child;
}

template aeXform* aeObject::newChild<aeXform>(const char* name, uint32_t tsi);
template aeCamera* aeObject::newChild<aeCamera>(const char* name, uint32_t tsi);
template aePolyMesh* aeObject::newChild<aePolyMesh>(const char* name, uint32_t tsi);
template aePoints* aeObject::newChild<aePoints>(const char* name, uint32_t tsi);

void aeObject::removeChild(aeObject* c)
{
    if (c == nullptr)
    { return; }

    auto it = std::find_if(m_children.begin(), m_children.end(), [c](const ObjectPtr& o)
    { return o.get() == c; });
    if (it != m_children.end())
    {
        c->m_parent = nullptr;
        m_children.erase(it);
    }
}

aeSchema::aeSchema(aeContext* ctx, aeObject* parent, abcObject* abc, uint32_t tsi)
    : super(ctx, parent, abc, tsi)
{
    m_visibility_prop = AbcGeom::CreateVisibilityProperty(*m_abc, tsi);
}

aeSchema::~aeSchema()
{
    m_properties.clear();
}

void aeSchema::markForceInvisible()
{
    m_force_invisible = true;
}

void aeSchema::writeVisibility(bool v)
{
    if (m_force_invisible)
    {
        m_force_invisible = false;
        v = false;
    }
    m_visibility_prop.set(v ? 1 : 0);
}

template<class T>
aeProperty* aeSchema::newProperty(const char* name, uint32_t tsi)
{
    auto cprop = getAbcProperties();
    if (!cprop.valid())
    {
        DebugLog("aeObject::newProperty() %s failed!", name);
        return nullptr;
    }

    auto* ret = aeMakeProperty<T>::make(this, name, tsi == 0 ? getTimeSamplingIndex() : tsi);
    m_properties.emplace_back(aePropertyPtr(ret));
    return ret;
}

#define Impl(T) template aeProperty* aeSchema::newProperty<T >(const char *name, uint32_t tsi)
Impl(abcBoolProperty);
Impl(abcIntProperty);
Impl(abcUIntProperty);
Impl(abcFloatProperty);
Impl(abcFloat2Property);
Impl(abcFloat3Property);
Impl(abcFloat4Property);
Impl(abcFloat4x4Property);

Impl(abcBoolArrayProperty);
Impl(abcIntArrayProperty);
Impl(abcUIntArrayProperty);
Impl(abcFloatArrayProperty);
Impl(abcFloat2ArrayProperty);
Impl(abcFloat3ArrayProperty);
Impl(abcFloat4ArrayProperty);
Impl(abcFloat4x4ArrayProperty);
#undef Impl
