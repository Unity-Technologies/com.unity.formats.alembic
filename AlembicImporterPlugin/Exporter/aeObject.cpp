#include "pch.h"
#include "AlembicExporter.h"
#include "aeObject.h"
#include "aeXForm.h"
#include "aePoints.h"
#include "aePolyMesh.h"
#include "aeCamera.h"

aeObject::aeObject(aeContext *ctx, AbcGeom::OObject &abc, const char *name)
    : m_ctx(ctx)
    , m_parent(nullptr)
    , m_abc(abc)
{

}

aeObject::aeObject(aeObject *parent, const char *name)
    : m_ctx(parent->m_ctx)
    , m_parent(parent)
    , m_abc(parent->m_abc, name)
{
}

aeObject::~aeObject()
{
}

const char* aeObject::getName() const           { return m_abc.getName().c_str(); }
const char* aeObject::getFullName() const       { return m_abc.getFullName().c_str(); }
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
    if (m_polymesh != nullptr) { return *m_polymesh; }

    m_polymesh.reset(new aePolyMesh(this));
    m_schemas.push_back(m_polymesh.get());
    return *m_polymesh;
}

aeCamera& aeObject::addCamera()
{
    if (m_camera != nullptr) { return *m_camera; }

    m_camera.reset(new aeCamera(this));
    m_schemas.push_back(m_camera.get());
    return *m_camera;
}

aePoints& aeObject::addPoints()
{
    if (m_xform != nullptr) { return *m_points; }

    m_points.reset(new aePoints(this));
    m_schemas.push_back(m_points.get());
    return *m_points;
}

