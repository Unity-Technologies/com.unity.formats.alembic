#include "pch.h"
#include "AlembicImporter.h"
#include "aiGeometry.h"
#include "aiContext.h"

#ifdef aiWindows
    #include <windows.h>
#endif // aiWindows

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

aiCLinkage aiExport bool aiXFormGetInherits(aiContextPtr ctx)
{
    return ctx->getXForm().getInherits();
}

aiCLinkage aiExport aiV3 aiXFormGetPosition(aiContextPtr ctx)
{
    return (aiV3&)ctx->getXForm().getPosition();
}

aiCLinkage aiExport aiV3 aiXFormGetAxis(aiContextPtr ctx)
{
    return (aiV3&)ctx->getXForm().getAxis();
}

aiCLinkage aiExport float aiXFormGetAngle(aiContextPtr ctx)
{
    return ctx->getXForm().getAngle();
}

aiCLinkage aiExport aiV3 aiXFormGetScale(aiContextPtr ctx)
{
    return (aiV3&)ctx->getXForm().getScale();
}

aiCLinkage aiExport aiM44 aiXFormGetMatrix(aiContextPtr ctx)
{
    return (aiM44&)ctx->getXForm().getMatrix();
}


aiCLinkage aiExport bool aiHasPolyMesh(aiContextPtr ctx)
{
    return ctx->hasPolyMesh();
}

aiCLinkage aiExport bool aiPolyMeshIsTopologyConstant(aiContextPtr ctx)
{
    return ctx->getPolyMesh().isTopologyConstant();
}

aiCLinkage aiExport bool aiPolyMeshIsTopologyConstantTriangles(aiContextPtr ctx)
{
    return ctx->getPolyMesh().isTopologyConstantTriangles();
}

aiCLinkage aiExport uint32_t aiPolyMeshGetIndexCount(aiContextPtr ctx)
{
    return ctx->getPolyMesh().getIndexCount();
}

aiCLinkage aiExport uint32_t aiPolyMeshGetVertexCount(aiContextPtr ctx)
{
    return ctx->getPolyMesh().getVertexCount();
}

aiCLinkage aiExport void aiPolyMeshCopyIndices(aiContextPtr ctx, int *dst)
{
    return ctx->getPolyMesh().copyIndices(dst);
}

aiCLinkage aiExport void aiPolyMeshCopyVertices(aiContextPtr ctx, abcV3 *dst)
{
    return ctx->getPolyMesh().copyVertices(dst);
}

aiCLinkage aiExport bool aiPolyMeshGetSplitedMeshInfo(aiContextPtr ctx, aiSplitedMeshInfo *o_smi, const aiSplitedMeshInfo *prev, int max_vertices)
{
    return ctx->getPolyMesh().getSplitedMeshInfo(*o_smi, *prev, max_vertices);
}

aiCLinkage aiExport void aiPolyMeshCopySplitedIndices(aiContextPtr ctx, int *dst, const aiSplitedMeshInfo *smi)
{
    return ctx->getPolyMesh().copySplitedIndices(dst, *smi);
}

aiCLinkage aiExport void aiPolyMeshCopySplitedVertices(aiContextPtr ctx, abcV3 *dst, const aiSplitedMeshInfo *smi)
{
    return ctx->getPolyMesh().copySplitedVertices(dst, *smi);
}


aiCLinkage aiExport bool aiHasCurves(aiContextPtr ctx)
{
    return ctx->hasCurves();
}


aiCLinkage aiExport bool aiHasPoints(aiContextPtr ctx)
{
    return ctx->hasPoints();
}


aiCLinkage aiExport bool aiHasCamera(aiContextPtr ctx)
{
    return ctx->hasCamera();
}

aiCLinkage aiExport void aiCameraGetParams(aiContextPtr ctx, aiCameraParams *o_params)
{
    ctx->getCamera().getParams(*o_params);
}


aiCLinkage aiExport bool aiHasMaterial(aiContextPtr ctx)
{
    return ctx->hasMaterial();

}
