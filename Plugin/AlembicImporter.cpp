#include "pch.h"
#include "AlembicImporter.h"
#include "aiGeometry.h"
#include "aiObject.h"
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







aiCLinkage aiExport aiContext* aiCreateContext()
{
    auto ctx = aiContext::create();
    aiDebugLog("aiCreateContext(): %p\n", ctx);
    return ctx;
}

aiCLinkage aiExport void aiDestroyContext(aiContext* ctx)
{
    aiDebugLog("aiDestroyContext(): %p\n", ctx);
    aiContext::destroy(ctx);
}


aiCLinkage aiExport bool aiLoad(aiContext* ctx, const char *path)
{
    aiDebugLog("aiLoad(): %p %s\n", ctx, path);
    return ctx->load(path);
}

aiCLinkage aiExport aiObject* aiGetTopObject(aiContext* ctx)
{
    return ctx->getTopObject();
}

aiCLinkage aiExport void aiEnumerateChild(aiObject *obj, aiNodeEnumerator e, void *userdata)
{
    //aiDebugLogVerbose("aiEnumerateChild(): %s (%d children)\n", obj->getName(), obj->getNumChildren());
    size_t n = obj->getNumChildren();
    for (size_t i = 0; i < n; ++i) {
        try {
            aiObject *child = obj->getChild(i);
            e(child, userdata);
        }
        catch (Alembic::Util::Exception e)
        {
            aiDebugLog("exception: %s\n", e.what());
        }
    }
}


aiCLinkage aiExport void aiSetCurrentTime(aiObject* obj, float time)
{
    //aiDebugLogVerbose("aiSetCurrentTime(): %.2f\n", time);
    obj->setCurrentTime(time);
}

aiCLinkage aiExport void aiEnableReverseX(aiObject* obj, bool v)
{
    obj->enableReverseX(v);
}

aiCLinkage aiExport void aiEnableTriangulate(aiObject* obj, bool v)
{
    obj->enableTriangulate(v);
}

aiCLinkage aiExport void aiEnableReverseIndex(aiObject* obj, bool v)
{
    obj->enableReverseIndex(v);
}



aiCLinkage aiExport const char* aiGetNameS(aiObject* obj)
{
    return obj->getName();
}

aiCLinkage aiExport const char* aiGetFullNameS(aiObject* obj)
{
    return obj->getFullName();
}

aiCLinkage aiExport uint32_t aiGetNumChildren(aiObject* obj)
{
    return obj->getNumChildren();
}


aiCLinkage aiExport bool aiHasXForm(aiObject* obj)
{
    return obj->hasXForm();
}

aiCLinkage aiExport bool aiXFormGetInherits(aiObject* obj)
{
    return obj->getXForm().getInherits();
}

aiCLinkage aiExport aiV3 aiXFormGetPosition(aiObject* obj)
{
    return (aiV3&)obj->getXForm().getPosition();
}

aiCLinkage aiExport aiV3 aiXFormGetAxis(aiObject* obj)
{
    return (aiV3&)obj->getXForm().getAxis();
}

aiCLinkage aiExport float aiXFormGetAngle(aiObject* obj)
{
    return obj->getXForm().getAngle();
}

aiCLinkage aiExport aiV3 aiXFormGetScale(aiObject* obj)
{
    return (aiV3&)obj->getXForm().getScale();
}

aiCLinkage aiExport aiM44 aiXFormGetMatrix(aiObject* obj)
{
    return (aiM44&)obj->getXForm().getMatrix();
}



aiCLinkage aiExport bool aiHasPolyMesh(aiObject* obj)
{
    return obj->hasPolyMesh();
}

aiCLinkage aiExport bool aiPolyMeshIsTopologyConstant(aiObject* obj)
{
    return obj->getPolyMesh().isTopologyConstant();
}

aiCLinkage aiExport bool aiPolyMeshIsTopologyConstantTriangles(aiObject* obj)
{
    return obj->getPolyMesh().isTopologyConstantTriangles();
}

aiCLinkage aiExport bool aiPolyMeshHasNormals(aiObject* obj)
{
    return obj->getPolyMesh().hasNormals();
}

aiCLinkage aiExport bool aiPolyMeshHasUVs(aiObject* obj)
{
    return obj->getPolyMesh().hasUVs();
}


aiCLinkage aiExport uint32_t aiPolyMeshGetIndexCount(aiObject* obj)
{
    return obj->getPolyMesh().getIndexCount();
}

aiCLinkage aiExport uint32_t aiPolyMeshGetVertexCount(aiObject* obj)
{
    return obj->getPolyMesh().getVertexCount();
}

aiCLinkage aiExport void aiPolyMeshCopyIndices(aiObject* obj, int *dst)
{
    return obj->getPolyMesh().copyIndices(dst);
}

aiCLinkage aiExport void aiPolyMeshCopyVertices(aiObject* obj, abcV3 *dst)
{
    return obj->getPolyMesh().copyVertices(dst);
}

aiCLinkage aiExport void aiPolyMeshCopyNormals(aiObject* obj, abcV3 *dst)
{
    return obj->getPolyMesh().copyNormals(dst);
}

aiCLinkage aiExport void aiPolyMeshCopyUVs(aiObject* obj, abcV2 *dst)
{
    return obj->getPolyMesh().copyUVs(dst);
}

aiCLinkage aiExport bool aiPolyMeshGetSplitedMeshInfo(aiObject* obj, aiSplitedMeshInfo *o_smi, const aiSplitedMeshInfo *prev, int max_vertices)
{
    return obj->getPolyMesh().getSplitedMeshInfo(*o_smi, *prev, max_vertices);
}

aiCLinkage aiExport void aiPolyMeshCopySplitedIndices(aiObject* obj, int *dst, const aiSplitedMeshInfo *smi)
{
    return obj->getPolyMesh().copySplitedIndices(dst, *smi);
}

aiCLinkage aiExport void aiPolyMeshCopySplitedVertices(aiObject* obj, abcV3 *dst, const aiSplitedMeshInfo *smi)
{
    return obj->getPolyMesh().copySplitedVertices(dst, *smi);
}

aiCLinkage aiExport void aiPolyMeshCopySplitedNormals(aiObject* obj, abcV3 *dst, const aiSplitedMeshInfo *smi)
{
    return obj->getPolyMesh().copySplitedNormals(dst, *smi);
}

aiCLinkage aiExport void aiPolyMeshCopySplitedUVs(aiObject* obj, abcV2 *dst, const aiSplitedMeshInfo *smi)
{
    return obj->getPolyMesh().copySplitedUVs(dst, *smi);
}


aiCLinkage aiExport bool aiHasCurves(aiObject* obj)
{
    return obj->hasCurves();
}


aiCLinkage aiExport bool aiHasPoints(aiObject* obj)
{
    return obj->hasPoints();
}


aiCLinkage aiExport bool aiHasCamera(aiObject* obj)
{
    return obj->hasCamera();
}

aiCLinkage aiExport void aiCameraGetParams(aiObject* obj, aiCameraParams *o_params)
{
    obj->getCamera().getParams(*o_params);
}


aiCLinkage aiExport bool aiHasMaterial(aiObject* obj)
{
    return obj->hasMaterial();

}
