#include "pch.h"
#include "AlembicImporter.h"
#include "aiLogger.h"
#include "aiContext.h"
#include "aiObject.h"
#include "Schema/aiSchema.h"
#include "Schema/aiXForm.h"
#include "Schema/aiPolyMesh.h"
#include "Schema/aiCamera.h"

aiObject::aiObject()
    : m_ctx(0)
{
}

aiObject::aiObject(aiContext *ctx, abcObject &abc)
    : m_ctx(ctx)
    , m_abc(abc)
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
    }
}

aiObject::~aiObject()
{
}

void aiObject::addChild(aiObject *c)
{
    m_children.push_back(c);
}

void aiObject::updateSample(float time)
{
    DebugLog("aiObject::updateSample(obj='%s', t=%f)", getFullName(), time);
    
    for (auto s : m_schemas)
    {
        s->updateSample(time);
    }
}

void aiObject::erasePastSamples(float from, float range)
{
    DebugLog("aiObject::erasePastSamples(obj='%s', from=%f, range=%f)", getFullName(), from, range);
    
    for (auto s : m_schemas)
    {
        s->erasePastSamples(from, range);
    }
}

aiContext*  aiObject::getContext()           { return m_ctx; }
abcObject&  aiObject::getAbcObject()         { return m_abc; }
const char* aiObject::getName() const        { return m_abc.getName().c_str(); }
const char* aiObject::getFullName() const    { return m_abc.getFullName().c_str(); }
uint32_t    aiObject::getNumChildren() const { return m_children.size(); }
aiObject*   aiObject::getChild(int i)        { return m_children[i]; }


bool aiObject::hasXForm() const    { return m_xform != nullptr; }
bool aiObject::hasPolyMesh() const { return m_polymesh != nullptr; }
bool aiObject::hasCamera() const   { return m_camera != nullptr; }

aiXForm&    aiObject::getXForm()      { return *m_xform; }
aiPolyMesh& aiObject::getPolyMesh()   { return *m_polymesh; }
aiCamera&   aiObject::getCamera()     { return *m_camera; }


