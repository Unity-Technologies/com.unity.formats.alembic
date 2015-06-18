#include "pch.h"
#include "AlembicImporter.h"
#include "aiGeometry.h"
#include "aiContext.h"
#include "aiObject.h"

aiObject::aiObject(aiContext *ctx, abcObject &abc)
    : m_ctx(ctx)
    , m_abc(abc)
    , m_time(0.0f)
    , m_reverse_x(true)
    , m_triangulate(true)
    , m_reverse_index(false)
{
#ifdef aiDebug
    m_magic = aiMagicObj;
#endif // aiDebug

    if (m_abc.valid())
    {
        const auto& metadata = m_abc.getMetaData();
        
        if (AbcGeom::IXformSchema::matches(metadata))
        {
            m_xform.reset(new aiXForm(this));
            m_schemas.push_back(&*m_xform);
        }
        
        if (AbcGeom::IPolyMeshSchema::matches(metadata))
        {
            m_polymesh.reset(new aiPolyMesh(this));
            m_schemas.push_back(&*m_polymesh);
        }
        
        if (AbcGeom::ICurvesSchema::matches(metadata))
        {
            m_curves.reset(new aiCurves(this));
            m_schemas.push_back(&*m_curves);
        }

        if (AbcGeom::IPointsSchema::matches(metadata))
        {
            m_points.reset(new aiPoints(this));
            m_schemas.push_back(&*m_points);
        }
        
        if (AbcGeom::ICameraSchema::matches(metadata))
        {
            m_camera.reset(new aiCamera(this));
            m_schemas.push_back(&*m_camera);
        }
        
        if (AbcGeom::ILight::matches(metadata))
        {
            m_light.reset(new aiLight(this));
            m_schemas.push_back(&*m_light);
        }
        
        if (AbcMaterial::IMaterial::matches(metadata))
        {
            m_material.reset(new aiMaterial(this));
            m_schemas.push_back(&*m_material);
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

aiContext*  aiObject::getContext()          { return m_ctx; }
abcObject&  aiObject::getAbcObject()        { return m_abc; }
const char* aiObject::getName() const       { return m_abc.getName().c_str(); }
const char* aiObject::getFullName() const   { return m_abc.getFullName().c_str(); }
uint32_t    aiObject::getNumChildren() const{ return m_children.size(); }
aiObject*   aiObject::getChild(int i)       { return m_children[i]; }

void aiObject::setCurrentTime(float time)
{
    m_time = time;
    for (auto s : m_schemas) {
        s->updateSample();
    }
}
void aiObject::enableReverseX(bool v)       { m_reverse_x = v; }
void aiObject::enableTriangulate(bool v)    { m_triangulate = v; }
void aiObject::enableReverseIndex(bool v)   { m_reverse_index = v; }

float aiObject::getCurrentTime() const      { return m_time; }
bool aiObject::getReverseX() const          { return m_reverse_x; }
bool aiObject::getReverseIndex() const      { return m_reverse_index; }
bool aiObject::getTriangulate() const       { return m_triangulate; }


bool aiObject::hasXForm() const    { return m_xform != nullptr; }
bool aiObject::hasPolyMesh() const { return m_polymesh != nullptr; }
bool aiObject::hasCurves() const   { return m_curves != nullptr; }
bool aiObject::hasPoints() const   { return m_points != nullptr; }
bool aiObject::hasCamera() const   { return m_camera != nullptr; }
bool aiObject::hasLight() const    { return m_light != nullptr; }
bool aiObject::hasMaterial() const { return m_material != nullptr; }

aiXForm&    aiObject::getXForm()      { return *m_xform; }
aiPolyMesh& aiObject::getPolyMesh()   { return *m_polymesh; }
aiCurves&   aiObject::getCurves()     { return *m_curves; }
aiPoints&   aiObject::getPoints()     { return *m_points; }
aiCamera&   aiObject::getCamera()     { return *m_camera; }
aiLight&    aiObject::getLight()      { return *m_light; }
aiMaterial& aiObject::getMaterial()   { return *m_material; }

void aiObject::debugDump() const
{
    if (!m_abc.valid()) return;

    aiDebugLog("node \"%s\"\n", getFullName());

    aiDebugLog("- metadata\n");
    const auto& metadata = m_abc.getMetaData();
    for (auto i : metadata) {
        aiDebugLog("%s: %s\n", i.first.c_str(), i.second.c_str());
    }

    for (auto &s : m_schemas) {
        s->debugDump();
    }

    aiDebugLog("\n");
}

