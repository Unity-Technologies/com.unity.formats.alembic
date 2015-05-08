#include "pch.h"
#include "UnityAlembicImporter.h"

class uaiContext;
typedef std::shared_ptr<uaiContext> uaiContextPtr;

class uaiContext
{
public:
    static int create();
    static void destroy(int ctx);
    static uaiContextPtr get(int ctx);

private:
    static std::map<int, uaiContextPtr> s_contexts;
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



uaiCLinkage uaiExport int uaiCreateContext()
{
    return uaiContext::create();
}


uaiCLinkage uaiExport void uaiDestroyContext(int ctx)
{
    uaiContext::destroy(ctx);
}


uaiCLinkage uaiExport bool uaiLoad(const char *path)
{
    Alembic::Abc::IArchive ia(Alembic::AbcCoreHDF5::ReadArchive(), path);
    return false;
}

uaiCLinkage uaiExport int uaiGetVertexCount(int ctx)
{
    return 0;
}

uaiCLinkage uaiExport void uaiFillVertex(int ctx, float *dst)
{
    ;
}

