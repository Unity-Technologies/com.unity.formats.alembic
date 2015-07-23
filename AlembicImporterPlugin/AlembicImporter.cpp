#include "pch.h"
#include "AlembicImporter.h"
#include "aiGeometry.h"
#include "aiObject.h"
#include "aiContext.h"
#include "aiLogger.h"

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



aiCLinkage aiExport void aiEnableFileLog(bool on, const char *path)
{
    aiLogger::Enable(on, path);
}

aiCLinkage aiExport aiContext* aiCreateContext(int uid)
{
    auto ctx = aiContext::create(uid);
    aiDebugLog("aiCreateContext(%d): %p\n", uid, ctx);
    return ctx;
}

aiCLinkage aiExport void aiDestroyContext(aiContext* ctx)
{
    aiCheckContext(ctx);
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


aiCLinkage aiExport void aiEnumerateChild(aiObject *obj, aiNodeEnumerator e, void *userData)
{
    aiCheckObject(obj);
    //aiDebugLogVerbose("aiEnumerateChild(): %s (%d children)\n", obj->getName(), obj->getNumChildren());
    size_t n = obj->getNumChildren();
    for (size_t i = 0; i < n; ++i) {
        try {
            aiObject *child = obj->getChild(i);
            e(child, userData);
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

aiCLinkage aiExport void aiEnableTriangulate(aiObject* obj, bool v)
{
    aiCheckObject(obj);
    obj->enableTriangulate(v);
}

aiCLinkage aiExport void aiSwapHandedness(aiObject* obj, bool v)
{
    aiCheckObject(obj);
    obj->swapHandedness(v);
}

aiCLinkage aiExport bool aiIsHandednessSwapped(aiObject* obj)
{
    aiCheckObject(obj);
    return obj->isHandednessSwapped();
}

aiCLinkage aiExport void aiSwapFaceWinding(aiObject* obj, bool v)
{
    aiCheckObject(obj);
    obj->swapFaceWinding(v);
}

aiCLinkage aiExport bool aiIsFaceWindingSwapped(aiObject* obj)
{
    aiCheckObject(obj);
    return obj->isFaceWindingSwapped();
}

aiCLinkage aiExport void aiSetNormalsMode(aiObject* obj, int m)
{
    aiCheckObject(obj);
    obj->setNormalsMode((aiNormalsMode) m);
}

aiCLinkage aiExport int aiGetNormalsMode(aiObject* obj)
{
    aiCheckObject(obj);
    return (int) obj->getNormalsMode();
}

aiCLinkage aiExport void aiSetTangentsMode(aiObject* obj, int m)
{
    aiCheckObject(obj);
    obj->setTangentsMode((aiTangentsMode) m);
}

aiCLinkage aiExport int aiGetTangentsMode(aiObject* obj)
{
    aiCheckObject(obj);
    return (int) obj->getTangentsMode();
}

aiCLinkage aiExport void aiCacheTangentsSplits(aiObject* obj, bool v)
{
    aiCheckObject(obj);
    obj->cacheTangentsSplits(v);
}

aiCLinkage aiExport bool aiAreTangentsSplitsCached(aiObject* obj)
{
    aiCheckObject(obj);
    return obj->areTangentsSplitsCached();
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

aiCLinkage aiExport int aiPolyMeshGetTopologyVariance(aiObject* obj)
{
    aiCheckObject(obj);
    return obj->getPolyMesh().getTopologyVariance();
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

aiCLinkage aiExport uint32_t aiPolyMeshGetSplitCount(aiObject *obj, bool force_refresh)
{
    aiCheckObject(obj);
    return obj->getPolyMesh().getSplitCount(force_refresh);
}

aiCLinkage aiExport uint32_t aiPolyMeshGetVertexBufferLength(aiObject* obj, uint32_t splitIndex)
{
    aiCheckObject(obj);
    return obj->getPolyMesh().getVertexBufferLength(splitIndex);
}

aiCLinkage aiExport void aiPolyMeshFillVertexBuffer(aiObject* obj, uint32_t splitIndex, abcV3 *positions, abcV3 *normals, abcV2 *uvs, abcV4 *tangents)
{
    aiCheckObject(obj);
    obj->getPolyMesh().fillVertexBuffer(splitIndex, positions, normals, uvs, tangents);
}

aiCLinkage aiExport uint32_t aiPolyMeshPrepareSubmeshes(aiObject* obj, const aiFacesets* facesets)
{
    aiCheckObject(obj);
    return obj->getPolyMesh().prepareSubmeshes(facesets);
}

aiCLinkage aiExport uint32_t aiPolyMeshGetSplitSubmeshCount(aiObject* obj, uint32_t splitIndex)
{
    aiCheckObject(obj);
    return obj->getPolyMesh().getSplitSubmeshCount(splitIndex);
}

aiCLinkage aiExport bool aiPolyMeshGetNextSubmesh(aiObject* obj, aiSubmeshInfo *smi)
{
    aiCheckObject(obj);
    return obj->getPolyMesh().getNextSubmesh(*smi);
}

aiCLinkage aiExport void aiPolyMeshFillSubmeshIndices(aiObject* obj, int *dst, const aiSubmeshInfo *smi)
{
    aiCheckObject(obj);
    obj->getPolyMesh().fillSubmeshIndices(dst, *smi);
}


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

aiCLinkage aiExport void aiCameraGetParams(aiObject* obj, aiCameraParams *params)
{
    aiCheckObject(obj);
    obj->getCamera().getParams(*params);
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
