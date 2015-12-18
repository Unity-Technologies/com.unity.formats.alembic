#include "pch.h"
#include "AlembicExporter.h"
#include "aeContext.h"
#include "aeObject.h"
#include "aeXForm.h"
#include "aePoints.h"
#include "aePolyMesh.h"
#include "aeCamera.h"

aeObject::aeObject(aeContext *ctx, aeObject *parent, AbcGeom::OObject *abc)
    : m_ctx(ctx)
    , m_parent(parent)
    , m_abc(abc)
{
}

aeObject::~aeObject()
{
    while (!m_children.empty()) {
        delete m_children.back();
    }
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
