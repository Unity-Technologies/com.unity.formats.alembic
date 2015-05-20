#include "pch.h"
#include "AlembicImporter.h"
#include "aiGeometry.h"
#include "aiContext.h"



aiContextPtr aiContext::create()
{
    return new aiContext();
}

void aiContext::destroy(aiContextPtr ctx)
{
    delete ctx;
}

aiContext::aiContext()
    : m_triangulate(true)
    , m_reverse_index(false)
    , m_has_xform(false)
    , m_has_polymesh(false)
    , m_has_curves(false)
    , m_has_points(false)
    , m_has_camera(false)
    , m_has_material(false)
{
}

bool aiContext::load(const char *path)
{
    if (path == nullptr) return false;

    try {
        aiDebugLog("trying to open AbcCoreHDF5::ReadArchive...\n");
        m_archive = abcArchivePtr(new Abc::IArchive(AbcCoreHDF5::ReadArchive(), path));
    }
    catch (Alembic::Util::Exception e)
    {
        aiDebugLog("exception: %s\n", e.what());

        try {
            aiDebugLog("trying to open AbcCoreOgawa::ReadArchive...\n");
            m_archive = abcArchivePtr(new Abc::IArchive(AbcCoreOgawa::ReadArchive(), path));
        }
        catch (Alembic::Util::Exception e)
        {
            aiDebugLog("exception: %s\n", e.what());
        }
    }

    if (m_archive && m_archive->valid()) {
        m_top_object = m_archive->getTop();
        aiDebugLog("succeeded\n");
        return true;
    }
    else {
        m_archive.reset();
        return false;
    }
}

abcObject* aiContext::getTopObject()
{
    return &m_top_object;
}

void aiContext::setCurrentObject(abcObject *obj)
{
    m_current = *obj;
    if (m_current.valid())
    {
#ifdef aiWithDebugLog
        m_dbg_current_object_name = obj->getFullName();
#endif // aiWithDebugLog

        const auto& metadata = m_current.getMetaData();
        m_has_xform = AbcGeom::IXformSchema::matches(metadata);
        m_has_polymesh = AbcGeom::IPolyMeshSchema::matches(metadata);
        m_has_curves = AbcGeom::ICurvesSchema::matches(metadata);
        m_has_points = AbcGeom::IPointsSchema::matches(metadata);
        m_has_camera = AbcGeom::ICameraSchema::matches(metadata);
        m_has_material = AbcMaterial::IMaterial::matches(metadata);

        if (m_has_xform)
        {
            m_xform = aiXForm(m_current, m_sample_selector);
        }
        if (m_has_polymesh)
        {
            m_polymesh = aiPolyMesh(m_current, m_sample_selector);
            m_polymesh.enableTriangulate(m_triangulate);
            m_polymesh.enableReverseIndex(m_reverse_index);
        }
        if (m_has_curves)
        {
            m_curves = aiCurves(m_current, m_sample_selector);
        }
        if (m_has_points)
        {
            m_points = aiPoints(m_current, m_sample_selector);
        }
        if (m_has_camera)
        {
            m_camera = aiCamera(m_current, m_sample_selector);
        }
        if (m_has_material)
        {
            m_material = aiMaterial(m_current, m_sample_selector);
        }

        //for (auto i : metadata) {
        //    aiDebugLogVerbose("%s: %s\n", i.first.c_str(), i.second.c_str());
        //}
    }
}

void aiContext::setCurrentTime(float time)
{
    m_sample_selector = Abc::ISampleSelector(time, Abc::ISampleSelector::kFloorIndex);
}

void aiContext::enableTriangulate(bool v)
{
    m_triangulate = v;
}

void aiContext::enableReverseIndex(bool v)
{
    m_reverse_index = v;
}

const char* aiContext::getName() const
{
    return m_current.getName().c_str();
}

const char* aiContext::getFullName() const
{
    return m_current.getFullName().c_str();
}

uint32_t aiContext::getNumChildren() const
{
    return m_current.getNumChildren();
}

bool aiContext::hasXForm() const    { return m_has_xform; }
bool aiContext::hasPolyMesh() const { return m_has_polymesh; }
bool aiContext::hasCurves() const   { return m_has_curves; }
bool aiContext::hasPoints() const   { return m_has_points; }
bool aiContext::hasCamera() const   { return m_has_camera; }
bool aiContext::hasMaterial() const { return m_has_material; }

aiXForm&    aiContext::getXForm()      { return m_xform; }
aiPolyMesh& aiContext::getPolyMesh()   { return m_polymesh; }
aiCurves&   aiContext::getCurves()     { return m_curves; }
aiPoints&   aiContext::getPoints()     { return m_points; }
aiCamera&   aiContext::getCamera()     { return m_camera; }
aiMaterial& aiContext::getMaterial()   { return m_material; }

