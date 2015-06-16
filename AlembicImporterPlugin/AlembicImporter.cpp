#include "pch.h"
#include "AlembicImporter.h"
#include "aiGeometry.h"
#include "aiObject.h"
#include "aiContext.h"

#ifdef aiWindows
    #include <windows.h>

#   define aiBreak() DebugBreak()
#else // aiWindows
#   define aiBreak() __builtin_trap()
#endif // aiWindows


#ifdef aiDebug
void aiDebugLogImpl(const char* fmt, ...)
{
    va_list vl;
    va_start(vl, fmt);

#ifdef aiWindows
    char buf[2048];
    vsprintf(buf, fmt, vl);
    ::OutputDebugStringA(buf);
#else // aiWindows
    vprintf(fmt, vl);
#endif // aiWindows

    va_end(vl);
}
#endif // aiDebug


#ifdef aiDebug
#define aiCheckContext(v) if(v==nullptr || *(int*)v!=aiMagicCtx) { aiBreak(); }
#define aiCheckObject(v)  if(v==nullptr || *(int*)v!=aiMagicObj) { aiBreak(); }
#else  // aiDebug
#define aiCheckContext(v) 
#define aiCheckObject(v)  
#endif // aiDebug





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
    aiCheckContext(ctx);
    aiDebugLog("aiLoad(): %p %s\n", ctx, path);
    return ctx->load(path);
}

aiCLinkage aiExport float aiGetStartTime(aiContext* ctx)
{
    aiCheckContext(ctx);
    aiDebugLog("aiGetStartTime(): %p\n", ctx);
    return ctx->getStartTime();
}

aiCLinkage aiExport float aiGetEndTime(aiContext* ctx)
{
    aiCheckContext(ctx);
    aiDebugLog("aiGetEndTime(): %p\n", ctx);
    return ctx->getEndTime();
}

aiCLinkage aiExport aiObject* aiGetTopObject(aiContext* ctx)
{
    aiCheckContext(ctx);
    return ctx->getTopObject();
}

aiCLinkage aiExport void aiWaitTasks(aiContext* ctx)
{
    ctx->waitTasks();
}


aiCLinkage aiExport void aiEnumerateChild(aiObject *obj, aiNodeEnumerator e, void *userdata)
{
    aiCheckObject(obj);
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
    aiCheckObject(obj);
    //aiDebugLogVerbose("aiSetCurrentTime(): %.2f\n", time);
    obj->setCurrentTime(time);
}

aiCLinkage aiExport void aiEnableReverseX(aiObject* obj, bool v)
{
    aiCheckObject(obj);
    obj->enableReverseX(v);
}

aiCLinkage aiExport void aiEnableTriangulate(aiObject* obj, bool v)
{
    aiCheckObject(obj);
    obj->enableTriangulate(v);
}

aiCLinkage aiExport void aiEnableReverseIndex(aiObject* obj, bool v)
{
    aiCheckObject(obj);
    obj->enableReverseIndex(v);
}



aiCLinkage aiExport const char* aiGetNameS(aiObject* obj)
{
    aiCheckObject(obj);
    return obj->getName();
}

aiCLinkage aiExport const char* aiGetFullNameS(aiObject* obj)
{
    aiCheckObject(obj);
    return obj->getFullName();
}

aiCLinkage aiExport uint32_t aiGetNumChildren(aiObject* obj)
{
    aiCheckObject(obj);
    return obj->getNumChildren();
}


aiCLinkage aiExport bool aiHasXForm(aiObject* obj)
{
    aiCheckObject(obj);
    return obj->hasXForm();
}

aiCLinkage aiExport bool aiXFormGetInherits(aiObject* obj)
{
    aiCheckObject(obj);
    return obj->getXForm().getInherits();
}

aiCLinkage aiExport aiV3 aiXFormGetPosition(aiObject* obj)
{
    aiCheckObject(obj);
    abcV3 p = obj->getXForm().getPosition();
    aiV3 rv = {p.x, p.y, p.z};
    return rv;
}

aiCLinkage aiExport aiV3 aiXFormGetAxis(aiObject* obj)
{
    aiCheckObject(obj);
    abcV3 a = obj->getXForm().getAxis();
    aiV3 rv = {a.x, a.y, a.z};
    return rv;
}

aiCLinkage aiExport float aiXFormGetAngle(aiObject* obj)
{
    aiCheckObject(obj);
    return obj->getXForm().getAngle();
}

aiCLinkage aiExport aiV3 aiXFormGetScale(aiObject* obj)
{
    aiCheckObject(obj);
    abcV3 s = obj->getXForm().getScale();
    aiV3 rv = {s.x, s.y, s.z};
    return rv;
}

aiCLinkage aiExport aiM44 aiXFormGetMatrix(aiObject* obj)
{
    aiCheckObject(obj);
    abcM44 m = obj->getXForm().getMatrix();
    aiM44 rv;
    for (int i=0; i<4; ++i)
        for (int j=0; j<4; ++j)
            rv.v[i][j] = m.x[i][j];
    return rv;
}



aiCLinkage aiExport bool aiHasPolyMesh(aiObject* obj)
{
    aiCheckObject(obj);
    return obj->hasPolyMesh();
}

aiCLinkage aiExport bool aiPolyMeshIsTopologyConstant(aiObject* obj)
{
    aiCheckObject(obj);
    return obj->getPolyMesh().isTopologyConstant();
}

aiCLinkage aiExport bool aiPolyMeshIsTopologyConstantTriangles(aiObject* obj)
{
    aiCheckObject(obj);
    return obj->getPolyMesh().isTopologyConstantTriangles();
}

aiCLinkage aiExport bool aiPolyMeshHasNormals(aiObject* obj)
{
    aiCheckObject(obj);
    return obj->getPolyMesh().hasNormals();
}

aiCLinkage aiExport bool aiPolyMeshHasUVs(aiObject* obj)
{
    aiCheckObject(obj);
    return obj->getPolyMesh().hasUVs();
}

aiCLinkage aiExport bool aiPolyMeshHasVelocities(aiObject* obj)
{
    aiCheckObject(obj);
    return obj->getPolyMesh().hasVelocities();
}


aiCLinkage aiExport uint32_t aiPolyMeshGetIndexCount(aiObject* obj)
{
    aiCheckObject(obj);
    return obj->getPolyMesh().getIndexCount();
}

aiCLinkage aiExport uint32_t aiPolyMeshGetVertexCount(aiObject* obj)
{
    aiCheckObject(obj);
    return obj->getPolyMesh().getVertexCount();
}

aiCLinkage aiExport uint32_t aiPolyMeshGetPeakIndexCount(aiObject* obj)
{
    aiCheckObject(obj);
    return obj->getPolyMesh().getPeakIndexCount();
}

aiCLinkage aiExport uint32_t aiPolyMeshGetPeakVertexCount(aiObject* obj)
{
    aiCheckObject(obj);
    return obj->getPolyMesh().getPeakVertexCount();
}

aiCLinkage aiExport void aiPolyMeshCopyIndices(aiObject* obj, int *dst)
{
    aiCheckObject(obj);
    return obj->getPolyMesh().copyIndices(dst);
}

aiCLinkage aiExport void aiPolyMeshCopyVertices(aiObject* obj, abcV3 *dst)
{
    aiCheckObject(obj);
    return obj->getPolyMesh().copyVertices(dst);
}

aiCLinkage aiExport void aiPolyMeshCopyNormals(aiObject* obj, abcV3 *dst)
{
    aiCheckObject(obj);
    return obj->getPolyMesh().copyNormals(dst);
}

aiCLinkage aiExport void aiPolyMeshCopyUVs(aiObject* obj, abcV2 *dst)
{
    aiCheckObject(obj);
    return obj->getPolyMesh().copyUVs(dst);
}

aiCLinkage aiExport bool aiPolyMeshGetSplitedMeshInfo(aiObject* obj, aiSplitedMeshInfo *o_smi, const aiSplitedMeshInfo *prev, int max_vertices)
{
    aiCheckObject(obj);
    return obj->getPolyMesh().getSplitedMeshInfo(*o_smi, *prev, max_vertices);
}

aiCLinkage aiExport void aiPolyMeshCopySplitedIndices(aiObject* obj, int *dst, const aiSplitedMeshInfo *smi)
{
    aiCheckObject(obj);
    return obj->getPolyMesh().copySplitedIndices(dst, *smi);
}

aiCLinkage aiExport void aiPolyMeshCopySplitedVertices(aiObject* obj, abcV3 *dst, const aiSplitedMeshInfo *smi)
{
    aiCheckObject(obj);
    return obj->getPolyMesh().copySplitedVertices(dst, *smi);
}

aiCLinkage aiExport void aiPolyMeshCopySplitedNormals(aiObject* obj, abcV3 *dst, const aiSplitedMeshInfo *smi)
{
    aiCheckObject(obj);
    return obj->getPolyMesh().copySplitedNormals(dst, *smi);
}

aiCLinkage aiExport void aiPolyMeshCopySplitedUVs(aiObject* obj, abcV2 *dst, const aiSplitedMeshInfo *smi)
{
    aiCheckObject(obj);
    return obj->getPolyMesh().copySplitedUVs(dst, *smi);
}


#ifdef aiSupportTextureMesh
aiCLinkage aiExport void aiPolyMeshCopyToTexture(aiObject* obj, aiTextureMeshData *dst)
{
    obj->getPolyMesh().copyMeshToTexture(*dst);
}
aiCLinkage aiExport void aiPolyMeshBeginCopyToTexture(aiObject* obj, aiTextureMeshData *dst)
{
    obj->getPolyMesh().beginCopyMeshToTexture(*dst);
}
aiCLinkage aiExport void aiPolyMeshEndCopyToTexture(aiObject* obj)
{
    obj->getPolyMesh().endCopyMeshToTexture();
}

#endif // aiSupportTextureMesh



aiCLinkage aiExport bool aiHasCurves(aiObject* obj)
{
    aiCheckObject(obj);
    return obj->hasCurves();
}


aiCLinkage aiExport bool aiHasPoints(aiObject* obj)
{
    aiCheckObject(obj);
    return obj->hasPoints();
}


aiCLinkage aiExport bool aiHasCamera(aiObject* obj)
{
    aiCheckObject(obj);
    return obj->hasCamera();
}

aiCLinkage aiExport void aiCameraGetParams(aiObject* obj, aiCameraParams *o_params)
{
    aiCheckObject(obj);
    obj->getCamera().getParams(*o_params);
}


aiCLinkage aiExport bool aiHasLight(aiObject* obj)
{
    aiCheckObject(obj);
    return obj->hasLight();
}


aiCLinkage aiExport bool aiHasMaterial(aiObject* obj)
{
    aiCheckObject(obj);
    return obj->hasMaterial();

}
