#include "pch.h"
#include "AlembicExporter.h"
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
typedef aeProperty super;
public:
    typedef T property_type;
    typedef typename T::value_type value_type;
    typedef typename T::sample_type sample_type;

    aeTArrayProprty(aeObject *parent, const char *name)
        : m_abcprop(new property_type(*parent->getAbcProperties(), name, parent->getContext()->getTimeSaplingIndex()))
    {
        aeDebugLog("aeTArrayProprty::aeTArrayProprty() %s", m_abcprop->getName().c_str());
    }
    const char* getName() const override { return m_abcprop->getName().c_str(); }
    bool isArray() const override { return true; }

    void writeSample(const void *data, int data_num) override
    {
        m_abcprop->set(sample_type((const value_type*)data, data_num));
    }

private:
    std::unique_ptr<property_type> m_abcprop;
};
template class aeTArrayProprty<abcFloatArrayProperty>;
template class aeTArrayProprty<abcInt32ArrayProperty>;
template class aeTArrayProprty<abcBoolArrayProperty >;
template class aeTArrayProprty<abcVec2ArrayProperty >;
template class aeTArrayProprty<abcVec3ArrayProperty >;
template class aeTArrayProprty<abcVec4ArrayProperty >;
template class aeTArrayProprty<abcMat44ArrayProperty>;


// scalar property

template<class T>
class aeTScalarProprty : public aeProperty
{
    typedef aeProperty super;
public:
    typedef T property_type;
    typedef typename T::value_type value_type;

    aeTScalarProprty(aeObject *parent, const char *name)
        : m_abcprop(new property_type(*parent->getAbcProperties(), name, parent->getContext()->getTimeSaplingIndex()))
    {
        aeDebugLog("aeTScalarProprty::aeTScalarProprty() %s", m_abcprop->getName().c_str());
    }
    const char* getName() const override { return m_abcprop->getName().c_str(); }
    bool isArray() const override { return false; }

    void writeSample(const void *data, int /*data_num*/) override
    {
        m_abcprop->set(*(const value_type*)data);
    }

private:
    std::unique_ptr<property_type> m_abcprop;
};
template class aeTScalarProprty<abcFloatProperty>;
template class aeTScalarProprty<abcInt32Property>;
template class aeTScalarProprty<abcBoolProperty >;
template class aeTScalarProprty<abcVec2Property >;
template class aeTScalarProprty<abcVec3Property >;
template class aeTScalarProprty<abcVec4Property >;
template class aeTScalarProprty<abcMat44Property>;


aeObject::aeObject(aeContext *ctx, aeObject *parent, AbcGeom::OObject *abc)
    : m_ctx(ctx)
    , m_parent(parent)
    , m_abc(abc)
{
    aeDebugLog("aeObject::aeObject() %s", getName());
}

aeObject::~aeObject()
{
    aeDebugLog("aeObject::~aeObject() %s", getName());

    while (!m_children.empty()) {
        delete m_children.back();
    }
    m_properties.clear();

    if (m_parent != nullptr) {
        m_parent->removeChild(this);
    }
}

const char* aeObject::getName() const           { return m_abc->getName().c_str(); }
const char* aeObject::getFullName() const       { return m_abc->getFullName().c_str(); }
uint32_t    aeObject::getNumChildren() const    { return m_children.size(); }
aeObject*   aeObject::getChild(int i)           { return m_children[i]; }
aeObject*   aeObject::getParent()               { return m_parent; }

aeContext*          aeObject::getContext()      { return m_ctx; }
const aeConfig&     aeObject::getConfig() const { return m_ctx->getConfig(); }
AbcGeom::OObject&   aeObject::getAbcObject()    { return *m_abc; }

template<class T>
T* aeObject::newChild(const char *name)
{
    T* child = new T(this, name);
    m_children.push_back(child);
    return child;
}
template aeXForm*       aeObject::newChild<aeXForm>(const char *name);
template aeCamera*      aeObject::newChild<aeCamera>(const char *name);
template aePolyMesh*    aeObject::newChild<aePolyMesh>(const char *name);
template aePoints*      aeObject::newChild<aePoints>(const char *name);

void aeObject::removeChild(aeObject *c)
{
    auto it = std::find(m_children.begin(), m_children.end(), c);
    if (it != m_children.end())
    {
        c->m_parent = nullptr;
        m_children.erase(it);
    }
}


abcProperties* aeObject::getAbcProperties()
{
    return nullptr;
}


template<class T, bool is_array=std::is_base_of<Abc::OArrayProperty, T>::value>
struct aeMakeProperty;

template<class T>
struct aeMakeProperty<T, true>
{
    static aeProperty* make(aeObject *parent, const char *name) { return new aeTArrayProprty<T>(parent, name); }
};

template<class T>
struct aeMakeProperty<T, false>
{
    static aeProperty* make(aeObject *parent, const char *name) { return new aeTScalarProprty<T>(parent, name); }
};

template<class T>
aeProperty* aeObject::newProperty(const char *name)
{
    auto *cprop = getAbcProperties();
    if (cprop == nullptr) {
        aeDebugLog("aeObject::newProperty() %s failed!", name);
        return nullptr;
    }

    auto *ret = aeMakeProperty<T>::make(this, name);
    m_properties.emplace_back(aePropertyPtr(ret));
    return ret;
}
template aeProperty*    aeObject::newProperty<abcFloatArrayProperty>(const char *name);
template aeProperty*    aeObject::newProperty<abcInt32ArrayProperty>(const char *name);
template aeProperty*    aeObject::newProperty<abcBoolArrayProperty >(const char *name);
template aeProperty*    aeObject::newProperty<abcVec2ArrayProperty >(const char *name);
template aeProperty*    aeObject::newProperty<abcVec3ArrayProperty >(const char *name);
template aeProperty*    aeObject::newProperty<abcVec4ArrayProperty >(const char *name);
template aeProperty*    aeObject::newProperty<abcMat44ArrayProperty>(const char *name);
template aeProperty*    aeObject::newProperty<abcFloatProperty>(const char *name);
template aeProperty*    aeObject::newProperty<abcInt32Property>(const char *name);
template aeProperty*    aeObject::newProperty<abcBoolProperty >(const char *name);
template aeProperty*    aeObject::newProperty<abcVec2Property >(const char *name);
template aeProperty*    aeObject::newProperty<abcVec3Property >(const char *name);
template aeProperty*    aeObject::newProperty<abcVec4Property >(const char *name);
template aeProperty*    aeObject::newProperty<abcMat44Property>(const char *name);
