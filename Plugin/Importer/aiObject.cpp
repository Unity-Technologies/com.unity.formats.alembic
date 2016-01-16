#include "pch.h"
#include "AlembicImporter.h"
#include "aiLogger.h"
#include "aiContext.h"
#include "aiObject.h"
#include "aiSchema.h"
#include "aiXForm.h"
#include "aiPolyMesh.h"
#include "aiCamera.h"
#include "aiPoints.h"

aiObject::aiObject()
    : m_ctx(nullptr)
    , m_parent(nullptr)
{
}

aiObject::aiObject(aiContext *ctx, aiObject *parent, abcObject &abc)
    : m_ctx(ctx)
    , m_abc(abc)
    , m_parent(parent)
{
    if (m_abc.valid())
    {
        const auto& metadata = m_abc.getMetaData();
        
        if (AbcGeom::IXformSchema::matches(metadata))
        {
            m_xform.reset(new aiXForm(this));
            m_schemas.push_back(m_xform.get());
        }
        
        if (AbcGeom::IPolyMeshSchema::matches(metadata))
        {
            m_polymesh.reset(new aiPolyMesh(this));
            m_schemas.push_back(m_polymesh.get());
        }

        if (AbcGeom::ICameraSchema::matches(metadata))
        {
            m_camera.reset(new aiCamera(this));
            m_schemas.push_back(m_camera.get());
        }

        if (AbcGeom::IPointsSchema::matches(metadata))
        {
            m_points.reset(new aiPoints(this));
            m_schemas.push_back(m_points.get());
        }
    }
}

aiObject::~aiObject()
{
    while (!m_children.empty()) {
        delete m_children.back();
    }
    if (m_parent != nullptr) {
        m_parent->removeChild(this);
    }
}

aiObject* aiObject::newChild(abcObject &abc)
{
    auto *child = new aiObject(getContext(), this, abc);
    m_children.push_back(child);
    return child;
}

void aiObject::removeChild(aiObject *c)
{
    if (c == nullptr) { return; }

    auto it = std::find(m_children.begin(), m_children.end(), c);
    if (it != m_children.end())
    {
        c->m_parent = 0;
        m_children.erase(it);
    }
}

void aiObject::readConfig()
{
    for (auto s : m_schemas)
    {
        s->readConfig();
    }
}

void aiObject::updateSample(float time)
{
    DebugLog("aiObject::updateSample(obj='%s', t=%f)", getFullName(), time);

    for (auto s : m_schemas)
    {
        s->updateSample(aiTimeToSampleSelector(time));
    }
}

void aiObject::notifyUpdate()
{
    for (auto s : m_schemas)
    {
        s->notifyUpdate();
    }
}

aiContext*  aiObject::getContext() { return m_ctx; }
abcObject&  aiObject::getAbcObject() { return m_abc; }
const char* aiObject::getName() const { return m_abc.getName().c_str(); }
const char* aiObject::getFullName() const { return m_abc.getFullName().c_str(); }
uint32_t    aiObject::getNumChildren() const { return (uint32_t)m_children.size(); }
aiObject*   aiObject::getChild(int i) { return m_children[i]; }
aiObject*   aiObject::getParent() { return m_parent; }

aiXForm*    aiObject::getXForm()      { return m_xform.get(); }
aiPolyMesh* aiObject::getPolyMesh()   { return m_polymesh.get(); }
aiCamera*   aiObject::getCamera()     { return m_camera.get(); }
aiPoints*   aiObject::getPoints()     { return m_points.get(); }


