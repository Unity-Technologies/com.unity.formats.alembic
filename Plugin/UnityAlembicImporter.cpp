#include "pch.h"
#include "UnityAlembicImporter.h"
#ifdef uaiWindows
    #include <windows.h>
#endif // uaiWindows

using namespace Alembic;

class uaiContext;
typedef std::shared_ptr<uaiContext> uaiContextPtr;
typedef std::shared_ptr<Abc::IArchive> abcArchivePtr;

#ifdef uaiWithDebugLog
void uaiDebugLog(const char* fmt, ...)
{
    va_list vl;
    va_start(vl, fmt);

#ifdef uaiWindows
    char buf[2048];
    vsprintf(buf, fmt, vl);
    ::OutputDebugStringA(buf);
#else // uaiWindows
    vprintf(vl);
#endif // uaiWindows

    va_end(vl);
}
#endif // uaiWithDebugLog


class uaiContext
{
public:
    static int create();
    static void destroy(int ctx);
    static uaiContextPtr get(int ctx);

    bool load(const char *path);
    abcObject* getTopObject();
    void setCurrentObject(abcObject *obj);

    const char* getName() const;
    const char* getFullName() const;
    uint32_t    getNumChildren() const;
    bool        hasXForm() const;
    bool        isPolyMesh() const;
    abcV3       getPosition() const;
    abcV3       getRotation() const;
    abcV3       getScale() const;
    abcM44      getMatrix() const;
    uint32_t    getVertexCount() const;
    uint32_t    getIndexCount() const;
    void        copyVertices(abcV3 *dst);
    void        copyIndices(int *dst);

private:
    static std::map<int, uaiContextPtr> s_contexts;

    abcArchivePtr m_archive;
    abcObject m_top_object;

    abcObject m_current;
    AbcGeom::XformSample m_xf;
    AbcGeom::IPolyMeshSchema::Sample m_mesh;
};

std::map<int, uaiContextPtr> uaiContext::s_contexts;

int uaiContext::create()
{
    static int s_idgen = 0;
    int id = ++s_idgen;
    s_contexts[id] = uaiContextPtr(new uaiContext());
    return id;
}

void uaiContext::destroy(int ctx)
{
    s_contexts.erase(ctx);
}

uaiContextPtr uaiContext::get(int ctx)
{
    auto v = s_contexts.find(ctx);
    return v == s_contexts.end() ? uaiContextPtr() : v->second;
}


bool uaiContext::load(const char *path)
{
    try {
        uaiDebugLog("trying to open AbcCoreHDF5::ReadArchive...\n");
        m_archive = abcArchivePtr(new Abc::IArchive(AbcCoreHDF5::ReadArchive(), path));
    }
    catch (Alembic::Util::Exception e)
    {
        uaiDebugLog("exception: %s\n", e.what());

        try {
            uaiDebugLog("trying to open AbcCoreOgawa::ReadArchive...\n");
            m_archive = abcArchivePtr(new Abc::IArchive(AbcCoreOgawa::ReadArchive(), path));
        }
        catch (Alembic::Util::Exception e)
        {
            uaiDebugLog("exception: %s\n", e.what());
        }
    }

    if (m_archive->valid()) {
        m_top_object = m_archive->getTop();
        uaiDebugLog("succeeded\n");
        return true;
    }
    else {
        m_archive.reset();
        return false;
    }
}

abcObject* uaiContext::getTopObject()
{
    return &m_top_object;
}

void uaiContext::setCurrentObject(abcObject *obj)
{
    m_current = *obj;
    if (m_current.valid())
    {
        if (hasXForm())
        {
            AbcGeom::IXform x(*obj, Abc::kWrapExisting);
            x.getSchema().get(m_xf);
        }

        if (isPolyMesh())
        {
            AbcGeom::IPolyMesh mesh(m_current.getParent(), m_current.getName());
            AbcGeom::IPolyMeshSchema schema = mesh.getSchema();
            schema.get(m_mesh);
        }
    }
}

const char* uaiContext::getName() const
{
    return m_current.getName().c_str();
}

const char* uaiContext::getFullName() const
{
    return m_current.getFullName().c_str();
}

uint32_t uaiContext::getNumChildren() const
{
    return m_current.getNumChildren();
}

bool uaiContext::hasXForm() const
{
    return AbcGeom::IXformSchema::matches(m_current.getMetaData());
}

bool uaiContext::isPolyMesh() const
{
    return AbcGeom::IPolyMeshSchema::matches(m_current.getMetaData());
}

abcV3 uaiContext::getPosition() const
{
    return abcV3(m_xf.getTranslation());
}

abcV3 uaiContext::getRotation() const
{
    return abcV3(m_xf.getXRotation(), m_xf.getYRotation(), m_xf.getZRotation());
}

abcV3 uaiContext::getScale() const
{
    return abcV3(m_xf.getScale());
}

abcM44 uaiContext::getMatrix() const
{
    return abcM44(m_xf.getMatrix());
}

uint32_t uaiContext::getVertexCount() const
{
    return m_mesh.getPositions()->size();
}

uint32_t uaiContext::getIndexCount() const
{
    return m_mesh.getFaceIndices()->size();
}

void uaiContext::copyVertices(abcV3 *dst)
{
    const auto &cont = *m_mesh.getPositions();
    size_t n = cont.size();
    for (size_t i = 0; i < n; ++i) {
        dst[i] = cont[i];
    }
}

void uaiContext::copyIndices(int *dst)
{
    const auto &cont = *m_mesh.getFaceIndices();
    size_t n = cont.size();
    for (size_t i = 0; i < n; ++i) {
        dst[i] = cont[i];
    }
}




uaiCLinkage uaiExport int uaiCreateContext()
{
    int ctx = uaiContext::create();
    uaiDebugLog("uaiCreateContext(): %d\n", ctx);
    return ctx;
}

uaiCLinkage uaiExport void uaiDestroyContext(int ctx)
{
    uaiDebugLog("uaiDestroyContext(): %d\n", ctx);
    uaiContext::destroy(ctx);
}


uaiCLinkage uaiExport bool uaiLoad(int ctx, const char *path)
{
    uaiDebugLog("uaiLoad(): %d %s\n", ctx, path);
    if (auto c = uaiContext::get(ctx))
    {
        return c->load(path);
    }
    return false;
}

uaiCLinkage uaiExport abcObject* uaiGetTopObject(int ctx)
{
    if (auto c = uaiContext::get(ctx))
    {
        return c->getTopObject();
    }
    return nullptr;
}

uaiCLinkage uaiExport void uaiEnumerateChild(int ctx, abcObject *obj, uaiNodeEnumerator e)
{
    uaiDebugLog("uaiEnumerateChild(): %d %s (%d children)\n", ctx, obj->getName().c_str(), obj->getNumChildren());
    if (auto c = uaiContext::get(ctx))
    {
        size_t n = obj->getNumChildren();
        for (size_t i = 0; i < n; ++i) {
            try {
                abcObject child(*obj, obj->getChildHeader(i).getName());
                c->setCurrentObject(&child);
                e(ctx, &child);
            }
            catch (Alembic::Util::Exception e)
            {
                uaiDebugLog("exception: %s\n", e.what());
            }
        }
    }
}


uaiCLinkage uaiExport void uaiSetCurrentObject(int ctx, abcObject *obj)
{
    if (auto c = uaiContext::get(ctx))
    {
        c->setCurrentObject(obj);
    }
}

uaiCLinkage uaiExport const char* uaiGetName(int ctx)
{
    if (auto c = uaiContext::get(ctx))
    {
        return c->getName();
    }
    return "";
}

uaiCLinkage uaiExport const char* uaiGetFullName(int ctx)
{
    if (auto c = uaiContext::get(ctx))
    {
        return c->getFullName();
    }
    return "";
}

uaiCLinkage uaiExport uint32_t uaiGetNumChildren(int ctx)
{
    if (auto c = uaiContext::get(ctx))
    {
        return c->getNumChildren();
    }
    return 0;
}

uaiCLinkage uaiExport bool uaiHasXForm(int ctx)
{
    if (auto c = uaiContext::get(ctx))
    {
        return c->hasXForm();
    }
    return false;
}

uaiCLinkage uaiExport bool uaiIsPolyMesh(int ctx)
{
    if (auto c = uaiContext::get(ctx))
    {
        return c->isPolyMesh();
    }
    return false;
}

uaiCLinkage uaiExport uaiV3 uaiGetPosition(int ctx)
{
    if (auto c = uaiContext::get(ctx))
    {
        return (uaiV3&)c->getPosition();
    }
    return uaiV3();
}

uaiCLinkage uaiExport uaiV3 uaiGetRotation(int ctx)
{
    if (auto c = uaiContext::get(ctx))
    {
        return (uaiV3&)c->getRotation();
    }
    return uaiV3();
}

uaiCLinkage uaiExport uaiV3 uaiGetScale(int ctx)
{
    if (auto c = uaiContext::get(ctx))
    {
        return (uaiV3&)c->getScale();
    }
    return uaiV3();
}

uaiCLinkage uaiExport uaiM44 uaiGetMatrix(int ctx)
{
    if (auto c = uaiContext::get(ctx))
    {
        return (uaiM44&)c->getMatrix();
    }
    return uaiM44();
}

uaiCLinkage uaiExport uint32_t uaiGetVertexCount(int ctx)
{
    if (auto c = uaiContext::get(ctx))
    {
        return c->getVertexCount();
    }
    return 0;
}

uaiCLinkage uaiExport uint32_t uaiGetIndexCount(int ctx)
{
    if (auto c = uaiContext::get(ctx))
    {
        return c->getIndexCount();
    }
    return 0;
}

uaiCLinkage uaiExport void uaiCopyMeshData(int ctx, abcV3 *vertices, int *indices)
{
    if (auto c = uaiContext::get(ctx))
    {
        return c->copyVertices(vertices);
        return c->copyIndices(indices);
    }
}
