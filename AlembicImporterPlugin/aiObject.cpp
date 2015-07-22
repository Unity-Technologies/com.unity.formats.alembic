#include "pch.h"
#include "AlembicImporter.h"
#include "aiGeometry.h"
#include "aiContext.h"
#include "aiObject.h"

aiObject::aiObject()
    : m_ctx(0)
    , m_hasXform(false)
    , m_hasPolymesh(false)
    , m_hasCurves(false)
    , m_hasPoints(false)
    , m_hasCamera(false)
    , m_hasLight(false)
    , m_hasMaterial(false)
    , m_time(0.0f)
    , m_triangulate(true)
    , m_swapHandedness(true)
    , m_swapFaceWinding(false)
    , m_normalsMode(NM_ComputeIfMissing)
    , m_tangentsMode(TM_None)
    , m_cacheTangentsSplits(true)
{
#ifdef aiDebug
    m_magic = aiMagicObj;
#endif // aiDebug
}

aiObject::aiObject(aiContext *ctx, abcObject &abc)
    : m_ctx(ctx)
    , m_abc(abc)
    , m_time(0.0f)
    , m_triangulate(true)
    , m_swapHandedness(true)
    , m_swapFaceWinding(false)
    , m_normalsMode(NM_ComputeIfMissing)
    , m_tangentsMode(TM_None)
    , m_cacheTangentsSplits(true)
{
#ifdef aiDebug
    m_magic = aiMagicObj;
#endif // aiDebug

    if (m_abc.valid())
    {
        const auto& metadata = m_abc.getMetaData();
        
        m_hasXform = AbcGeom::IXformSchema::matches(metadata);
        if (m_hasXform)
        {
            m_xform = aiXForm(this);
            m_schemas.push_back(&m_xform);
        }
        
        m_hasPolymesh = AbcGeom::IPolyMeshSchema::matches(metadata);
        if (m_hasPolymesh)
        {
            m_polymesh = aiPolyMesh(this);
            m_schemas.push_back(&m_polymesh);
        }
        
        m_hasCurves = AbcGeom::ICurvesSchema::matches(metadata);
        if (m_hasCurves)
        {
            m_curves = aiCurves(this);
            m_schemas.push_back(&m_curves);
        }
        
        m_hasPoints = AbcGeom::IPointsSchema::matches(metadata);
        if (m_hasPoints)
        {
            m_points = aiPoints(this);
            m_schemas.push_back(&m_points);
        }
        
        m_hasCamera = AbcGeom::ICameraSchema::matches(metadata);
        if (m_hasCamera)
        {
            m_camera = aiCamera(this);
            m_schemas.push_back(&m_camera);
        }
        
        m_hasLight = AbcGeom::ILight::matches(metadata);
        if (m_hasLight)
        {
            m_light = aiLight(this);
            m_schemas.push_back(&m_light);
        }
        
        m_hasMaterial = AbcMaterial::IMaterial::matches(metadata);
        if (m_hasMaterial)
        {
            m_material = aiMaterial(this);
            m_schemas.push_back(&m_material);
        }

        //for (auto i : metadata) {
        //    aiDebugLogVerbose("%s: %s\n", i.first.c_str(), i.second.c_str());
        //}
    }
}

aiObject::~aiObject()
{
}

void aiObject::addChild(aiObject *c)
{
    m_children.push_back(c);
}

aiContext*  aiObject::getContext()           { return m_ctx; }
abcObject&  aiObject::getAbcObject()         { return m_abc; }
const char* aiObject::getName() const        { return m_abc.getName().c_str(); }
const char* aiObject::getFullName() const    { return m_abc.getFullName().c_str(); }
uint32_t    aiObject::getNumChildren() const { return m_children.size(); }
aiObject*   aiObject::getChild(int i)        { return m_children[i]; }

void aiObject::setCurrentTime(float time)
{
    m_time = time;
    for (auto s : m_schemas) {
        s->updateSample();
    }
}

void aiObject::enableTriangulate(bool v)         { m_triangulate = v; }
void aiObject::swapHandedness(bool v)            { m_swapHandedness = v; }
void aiObject::swapFaceWinding(bool v)           { m_swapFaceWinding = v; }
void aiObject::setNormalsMode(aiNormalsMode m)   { m_normalsMode = m; }
void aiObject::setTangentsMode(aiTangentsMode m) { m_tangentsMode = m; }
void aiObject::cacheTangentsSplits(bool v)       { m_cacheTangentsSplits = v; }

float aiObject::getCurrentTime() const           { return m_time; }
bool aiObject::getTriangulate() const            { return m_triangulate; }
bool aiObject::isHandednessSwapped() const       { return m_swapHandedness; }
bool aiObject::isFaceWindingSwapped() const      { return m_swapFaceWinding; }
aiNormalsMode aiObject::getNormalsMode() const   { return m_normalsMode; }
aiTangentsMode aiObject::getTangentsMode() const { return m_tangentsMode; }
bool aiObject::areTangentsSplitsCached() const   { return m_cacheTangentsSplits; }


bool aiObject::hasXForm() const    { return m_hasXform; }
bool aiObject::hasPolyMesh() const { return m_hasPolymesh; }
bool aiObject::hasCurves() const   { return m_hasCurves; }
bool aiObject::hasPoints() const   { return m_hasPoints; }
bool aiObject::hasCamera() const   { return m_hasCamera; }
bool aiObject::hasLight() const    { return m_hasLight; }
bool aiObject::hasMaterial() const { return m_hasMaterial; }

aiXForm&    aiObject::getXForm()      { return m_xform; }
aiPolyMesh& aiObject::getPolyMesh()   { return m_polymesh; }
aiCurves&   aiObject::getCurves()     { return m_curves; }
aiPoints&   aiObject::getPoints()     { return m_points; }
aiCamera&   aiObject::getCamera()     { return m_camera; }
aiLight&    aiObject::getLight()      { return m_light; }
aiMaterial& aiObject::getMaterial()   { return m_material; }


