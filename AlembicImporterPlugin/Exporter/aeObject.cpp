#include "pch.h"
#include "AlembicExporter.h"
#include "aeObject.h"
#include "aeXForm.h"
#include "aePoints.h"
#include "aePolyMesh.h"
#include "aeCamera.h"

aeObject::aeObject(aeContext *ctx, AbcGeom::OObject &abc, const char *name)
    : m_ctx(ctx)
    , m_abc(abc)
{

}

aeObject::~aeObject()
{

}

const char* aeObject::getName() const
{

}

const char* aeObject::getFullName() const
{

}

uint32_t    aeObject::getNumChildren() const    { return m_children.size(); }
aeObject*   aeObject::getChild(int i)           { return m_children[i]; }
aeObject*   aeObject::getParent()               { return m_parent; }

aeContext*          aeObject::getContext() { return m_ctx; }
AbcGeom::OObject&   aeObject::getAbcObject() { return m_abc; }
void                aeObject::addChild(aeObject *c) { m_children.push_back(c); }
void aeObject::removeChild(aeObject *c)
{
    auto it = std::find(m_children.begin(), m_children.end(), c);
    if (it != m_children.end())
    {
        c->m_parent = nullptr;
        m_children.erase(it);
    }
}

aeXForm& aeObject::addXForm()
{
    if (m_xform != nullptr) { return *m_xform; }

    m_xform.reset(new aeXForm(this));
    m_schemas.push_back(m_xform.get());
    return *m_xform;
}

aePolyMesh& aeObject::addPolyMesh()
{
}

aeCamera& aeObject::addCamera()
{
}

aePoints& aeObject::addPoints()
{
}

