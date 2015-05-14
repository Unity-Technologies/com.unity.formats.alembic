#include "pch.h"
#include "AlembicImporter.h"
#ifdef aiWindows
    #include <windows.h>
#endif // aiWindows

using namespace Alembic;

typedef std::shared_ptr<Abc::IArchive> abcArchivePtr;

#ifdef aiWithDebugLog
void aiDebugLogImpl(const char* fmt, ...)
{
    va_list vl;
    va_start(vl, fmt);

#ifdef aiWindows
    char buf[2048];
    vsprintf(buf, fmt, vl);
    ::OutputDebugStringA(buf);
#else // aiWindows
    vprintf(vl);
#endif // aiWindows

    va_end(vl);
}
#endif // aiWithDebugLog


class aiContext
{
public:
    static aiContextPtr create();
    static void destroy(aiContextPtr ctx);

    aiContext();
    bool load(const char *path);
    abcObject* getTopObject();
    void setCurrentObject(abcObject *obj);
    void setCurrentTime(float time);
    void enableTriangulate(bool v);
    void enableReverseIndex(bool v);

    const char* getName() const;
    const char* getFullName() const;
    uint32_t    getNumChildren() const;

    bool        hasXForm() const;
    abcV3       getPosition() const;
    abcV3       getRotation() const;
    abcV3       getScale() const;
    abcM44      getMatrix() const;

    bool        hasPolyMesh() const;
    bool        isNormalIndexed() const;
    bool        isUVIndexed() const;
    uint32_t    getIndexCount() const;
    uint32_t    getVertexCount() const;
    void        copyIndices(int *dst) const;
    void        copyVertices(abcV3 *dst) const;
    bool        getSplitedMeshInfo(aiSplitedMeshInfo &o_sp, const aiSplitedMeshInfo& prev, int max_vertices) const;
    void        copySplitedIndices(int *dst, const aiSplitedMeshInfo &smi);
    void        copySplitedVertices(abcV3 *dst, const aiSplitedMeshInfo &smi);

private:
#ifdef aiWithDebugLog
    std::string m_dbg_current_object_name;
#endif // aiWithDebugLog
    abcArchivePtr m_archive;
    abcObject m_top_object;

    abcObject m_current;
    Abc::ISampleSelector m_sample_selector;
    AbcGeom::XformSample m_xf;
    AbcGeom::IPolyMeshSchema m_mesh_schema;
    Abc::Int32ArraySamplePtr m_indices;
    Abc::Int32ArraySamplePtr m_counts;
    Abc::P3fArraySamplePtr m_positions;
    Abc::V3fArraySamplePtr m_velocities;
    bool m_triangulate;
    bool m_reverse_index;
};


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

    if (m_archive->valid()) {
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

        if (hasXForm())
        {
            AbcGeom::IXform x(*obj, Abc::kWrapExisting);
            x.getSchema().get(m_xf);
        }

        if (hasPolyMesh())
        {
            AbcGeom::IPolyMesh mesh(m_current.getParent(), m_current.getName());
            m_mesh_schema = mesh.getSchema();
            m_mesh_schema.getFaceIndicesProperty().get(m_indices, m_sample_selector);
            m_mesh_schema.getFaceCountsProperty().get(m_counts, m_sample_selector);
            m_mesh_schema.getPositionsProperty().get(m_positions, m_sample_selector);
            if (m_mesh_schema.getVelocitiesProperty().valid()) {
                m_mesh_schema.getVelocitiesProperty().get(m_velocities, m_sample_selector);
            }
            if (m_mesh_schema.isConstant()) {
                aiDebugLog("warning: topology is not consant\n");
            }
        }
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


bool aiContext::hasXForm() const
{
    return AbcGeom::IXformSchema::matches(m_current.getMetaData());
}

abcV3 aiContext::getPosition() const
{
    return abcV3(m_xf.getTranslation());
}

abcV3 aiContext::getRotation() const
{
    return abcV3(m_xf.getXRotation(), m_xf.getYRotation(), m_xf.getZRotation());
}

abcV3 aiContext::getScale() const
{
    return abcV3(m_xf.getScale());
}

abcM44 aiContext::getMatrix() const
{
    return abcM44(m_xf.getMatrix());
}


bool aiContext::hasPolyMesh() const
{
    return AbcGeom::IPolyMeshSchema::matches(m_current.getMetaData());
}
bool aiContext::isNormalIndexed() const
{
    return m_mesh_schema.getNormalsParam().isIndexed();
}

bool aiContext::isUVIndexed() const
{
    return m_mesh_schema.getUVsParam().isIndexed();
}

uint32_t aiContext::getIndexCount() const
{
    if (m_triangulate)
    {
        uint32_t r = 0;
        const auto &counts = *m_counts;
        size_t n = counts.size();
        for (size_t fi = 0; fi < n; ++fi) {
            int ngon = counts[fi];
            r += (ngon - 2) * 3;
        }
        return r;
    }
    else
    {
        return m_indices->size();
    }
}

uint32_t aiContext::getVertexCount() const
{
    return m_positions->size();
}

void aiContext::copyIndices(int *dst) const
{
    const auto &counts = *m_counts;
    const auto &indices = *m_indices;

    if (m_triangulate)
    {
        uint32_t a = 0;
        uint32_t b = 0;
        uint32_t i1 = m_reverse_index ? 2 : 1;
        uint32_t i2 = m_reverse_index ? 1 : 2;
        size_t n = counts.size();
        for (size_t fi = 0; fi < n; ++fi) {
            int ngon = counts[fi];
            for (int ni = 0; ni < (ngon - 2); ++ni) {
                dst[b + 0] = std::max<int>(indices[a], 0);
                dst[b + 1] = std::max<int>(indices[a + i1 + ni], 0);
                dst[b + 2] = std::max<int>(indices[a + i2 + ni], 0);
                b += 3;
            }
            a += ngon;
        }
    }
    else
    {
        size_t n = indices.size();
        if (m_reverse_index) {
            for (size_t i = 0; i < n; ++i) {
                dst[i] = indices[n - i - 1];
            }
        }
        else {
            for (size_t i = 0; i < n; ++i) {
                dst[i] = indices[i];
            }
        }
    }
}

void aiContext::copyVertices(abcV3 *dst) const
{
    const auto &cont = *m_positions;
    size_t n = cont.size();
    for (size_t i = 0; i < n; ++i) {
        dst[i] = cont[i];
    }
}

bool aiContext::getSplitedMeshInfo(aiSplitedMeshInfo &o_smi, const aiSplitedMeshInfo& prev, int max_vertices) const
{
    const auto &counts = *m_counts;
    const auto &indices = *m_indices;
    const auto &positions = *m_positions;
    size_t nc = counts.size();
    size_t ni = indices.size();

    aiSplitedMeshInfo smi = {0};
    smi.begin_face = prev.begin_face + prev.num_faces;
    smi.begin_index = prev.begin_index + prev.num_indices;

    bool is_end = true;
    uint32_t a = 0;
    for (size_t i = smi.begin_face; i < nc; ++i) {
        int ngon = counts[i];
        if (a + ngon >= max_vertices) {
            is_end = false;
            break;
        }

        a += ngon;
        smi.num_faces++;
        smi.num_indices = a;
        smi.triangulated_index_count += (ngon - 2) * 3;
    }

    smi.num_vertices = a;
    o_smi = smi;
    return is_end;
}

void aiContext::copySplitedIndices(int *dst, const aiSplitedMeshInfo &smi)
{
    const auto &counts = *m_counts;
    const auto &indices = *m_indices;

    uint32_t a = 0;
    uint32_t b = 0;
    uint32_t i1 = m_reverse_index ? 2 : 1;
    uint32_t i2 = m_reverse_index ? 1 : 2;
    for (size_t fi = 0; fi < smi.num_faces; ++fi) {
        int ngon = counts[smi.begin_face + fi];
        for (int ni = 0; ni < (ngon - 2); ++ni) {
            dst[b + 0] = a;
            dst[b + 1] = a + i1 + ni;
            dst[b + 2] = a + i2 + ni;
            b += 3;
        }
        a += ngon;
    }
}

void aiContext::copySplitedVertices(abcV3 *dst, const aiSplitedMeshInfo &smi)
{
    const auto &counts = *m_counts;
    const auto &indices = *m_indices;
    const auto &positions = *m_positions;

    uint32_t a = 0;
    uint32_t i1 = m_reverse_index ? 2 : 1;
    uint32_t i2 = m_reverse_index ? 1 : 2;
    for (size_t fi = 0; fi < smi.num_faces; ++fi) {
        int ngon = counts[smi.begin_face + fi];
        for (int ni = 0; ni < ngon; ++ni) {
            dst[a + ni] = positions[indices[a + ni + smi.begin_index]];
        }
        a += ngon;
    }
}




aiCLinkage aiExport aiContextPtr aiCreateContext()
{
    auto ctx = aiContext::create();
    aiDebugLog("aiCreateContext(): %p\n", ctx);
    return ctx;
}

aiCLinkage aiExport void aiDestroyContext(aiContextPtr ctx)
{
    aiDebugLog("aiDestroyContext(): %p\n", ctx);
    aiContext::destroy(ctx);
}


aiCLinkage aiExport bool aiLoad(aiContextPtr ctx, const char *path)
{
    aiDebugLog("aiLoad(): %p %s\n", ctx, path);
    return ctx->load(path);
}

aiCLinkage aiExport abcObject* aiGetTopObject(aiContextPtr ctx)
{
    return ctx->getTopObject();
}

aiCLinkage aiExport void aiEnumerateChild(aiContextPtr ctx, abcObject *obj, aiNodeEnumerator e, void *userdata)
{
    aiDebugLogVerbose("aiEnumerateChild(): %p %s (%d children)\n", ctx, obj->getName().c_str(), obj->getNumChildren());
    size_t n = obj->getNumChildren();
    for (size_t i = 0; i < n; ++i) {
        try {
            abcObject child(*obj, obj->getChildHeader(i).getName());
            ctx->setCurrentObject(&child);
            e(ctx, &child, userdata);
        }
        catch (Alembic::Util::Exception e)
        {
            aiDebugLog("exception: %s\n", e.what());
        }
    }
}


aiCLinkage aiExport void aiSetCurrentObject(aiContextPtr ctx, abcObject *obj)
{
    ctx->setCurrentObject(obj);
}

aiCLinkage aiExport void aiSetCurrentTime(aiContextPtr ctx, float time)
{
    aiDebugLogVerbose("aiSetCurrentTime(): %p %.2f\n", ctx, time);
    ctx->setCurrentTime(time);
}

aiCLinkage aiExport void aiEnableTriangulate(aiContextPtr ctx, bool v)
{
    ctx->enableTriangulate(v);
}

aiCLinkage aiExport void aiEnableReverseIndex(aiContextPtr ctx, bool v)
{
    ctx->enableReverseIndex(v);
}


aiCLinkage aiExport const char* aiGetNameS(aiContextPtr ctx)
{
    return ctx->getName();
}

aiCLinkage aiExport const char* aiGetFullNameS(aiContextPtr ctx)
{
    return ctx->getFullName();
}

aiCLinkage aiExport uint32_t aiGetNumChildren(aiContextPtr ctx)
{
    return ctx->getNumChildren();
}


aiCLinkage aiExport bool aiHasXForm(aiContextPtr ctx)
{
    return ctx->hasXForm();
}

aiCLinkage aiExport aiV3 aiGetPosition(aiContextPtr ctx)
{
    return (aiV3&)ctx->getPosition();
}

aiCLinkage aiExport aiV3 aiGetRotation(aiContextPtr ctx)
{
    return (aiV3&)ctx->getRotation();
}

aiCLinkage aiExport aiV3 aiGetScale(aiContextPtr ctx)
{
    return (aiV3&)ctx->getScale();
}

aiCLinkage aiExport aiM44 aiGetMatrix(aiContextPtr ctx)
{
    return (aiM44&)ctx->getMatrix();
}


aiCLinkage aiExport bool aiHasPolyMesh(aiContextPtr ctx)
{
    return ctx->hasPolyMesh();
}
aiCLinkage aiExport uint32_t aiGetIndexCount(aiContextPtr ctx)
{
    return ctx->getIndexCount();
}

aiCLinkage aiExport uint32_t aiGetVertexCount(aiContextPtr ctx)
{
    return ctx->getVertexCount();
}

aiCLinkage aiExport void aiCopyIndices(aiContextPtr ctx, int *dst)
{
    return ctx->copyIndices(dst);
}

aiCLinkage aiExport void aiCopyVertices(aiContextPtr ctx, abcV3 *dst)
{
    return ctx->copyVertices(dst);
}

aiCLinkage aiExport bool aiGetSplitedMeshInfo(aiContextPtr ctx, aiSplitedMeshInfo *o_smi, const aiSplitedMeshInfo *prev, int max_vertices)
{
    return ctx->getSplitedMeshInfo(*o_smi, *prev, max_vertices);
}

aiCLinkage aiExport void aiCopySplitedIndices(aiContextPtr ctx, int *dst, const aiSplitedMeshInfo *smi)
{
    return ctx->copySplitedIndices(dst, *smi);
}

aiCLinkage aiExport void aiCopySplitedVertices(aiContextPtr ctx, abcV3 *dst, const aiSplitedMeshInfo *smi)
{
    return ctx->copySplitedVertices(dst, *smi);
}
