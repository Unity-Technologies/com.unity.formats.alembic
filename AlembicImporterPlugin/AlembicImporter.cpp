#include "pch.h"
#include "AlembicImporter.h"
#include "Schema/aiSchema.h"
#include "Schema/aiXForm.h"
#include "Schema/aiPolyMesh.h"
#include "Schema/aiCamera.h"
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


aiCLinkage aiExport void aiCleanup()
{
#ifdef aiWithTBB
#else
    aiThreadPool::releaseInstance();
#endif
}

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

aiCLinkage aiExport void aiSetImportConfig(aiContext* ctx, const aiImportConfig *conf)
{
    ctx->setImportConfig(*conf);
}

aiCLinkage aiExport void aiDebugDump(aiContext* ctx)
{
    aiDebugLog("aiDebugDump(): %p\n", ctx);
    ctx->debugDump();
}

aiCLinkage aiExport float aiGetStartTime(aiContext* ctx)
{
    aiDebugLog("aiGetStartTime(): %p\n", ctx);
    return ctx->getStartTime();
}

aiCLinkage aiExport float aiGetEndTime(aiContext* ctx)
{
    aiDebugLog("aiGetEndTime(): %p\n", ctx);
    return ctx->getEndTime();
}

aiCLinkage aiExport aiObject* aiGetTopObject(aiContext* ctx)
{
    return ctx->getTopObject();
}

aiCLinkage aiExport void aiUpdateSamples(aiContext* ctx, float time)
{
    ctx->updateSamples(time);
}
aiCLinkage aiExport void aiUpdateSamplesBegin(aiContext* ctx, float time)
{
    ctx->updateSamplesBegin(time);
}
aiCLinkage aiExport void aiUpdateSamplesEnd(aiContext* ctx)
{
    ctx->updateSamplesEnd();
}
aiCLinkage aiExport void aiSetTimeRangeToKeepSamples(aiContext *ctx, float time, float range)
{
    ctx->setTimeRangeToKeepSamples(time, range);
}
aiCLinkage aiExport void aiErasePastSamples(aiContext* ctx, float time, float range)
{
    ctx->erasePastSamples(time, range);
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



aiCLinkage aiExport const char* aiGetNameS(aiObject* obj)
{
    return obj->getName();
}

aiCLinkage aiExport const char* aiGetFullNameS(aiObject* obj)
{
    return obj->getFullName();
}



aiCLinkage aiExport void aiSchemaSetCallback(aiSchemaBase* schema, aiSampleCallback cb, void *arg)
{
    schema->setCallback(cb, arg);
}

aiCLinkage aiExport const aiSampleBase* aiSchemaUpdateSample(aiSchemaBase* schema, float time)
{
    return schema->updateSample(time);
}

aiCLinkage aiExport const aiSampleBase* aiSchemaGetSample(aiSchemaBase* schema, float time)
{
    return schema->findSample(time);
}

aiCLinkage aiExport uint32_t aiSampleGetTime(aiSampleBase* sample)
{
    return sample->getTime();
}



aiCLinkage aiExport bool aiHasXForm(aiObject* obj)
{
    return obj->hasXForm();
}

aiCLinkage aiExport aiXForm* aiGetXForm(aiObject* obj)
{
    return &obj->getXForm();
}

aiCLinkage aiExport void aiXFormGetData(aiXFormSample* sample, aiXFormData *o_data)
{
    if (!sample) { return; }
    sample->getData(*o_data);
}


aiCLinkage aiExport bool aiHasPolyMesh(aiObject* obj)
{
    return obj->hasPolyMesh();
}

aiCLinkage aiExport aiPolyMesh* aiGetPolyMesh(aiObject* obj)
{
    return &obj->getPolyMesh();
}

aiCLinkage aiExport int aiPolyMeshGetTopologyVariance(aiPolyMesh* schema)
{
    return schema->getTopologyVariance();
}

aiCLinkage aiExport uint32_t aiPolyMeshGetPeakIndexCount(aiPolyMesh* schema)
{
    return schema->getPeakIndexCount();
}

aiCLinkage aiExport uint32_t aiPolyMeshGetPeakVertexCount(aiPolyMesh* schema)
{
    return schema->getPeakVertexCount();
}

aiCLinkage aiExport void aiPolyMeshGetSchemaSummary(aiPolyMesh* schema, aiPolyMeshSchemaSummary *o_summary)
{
    schema->getSummary(*o_summary);
}

aiCLinkage aiExport void aiPolyMeshGetSampleSummary(aiPolyMeshSample* sample, aiPolyMeshSampleSummary *o_summary)
{
    if (!sample) { return; }
    sample->getSummary(*o_summary);
}

aiCLinkage aiExport bool aiPolyMeshGetSplitedMeshInfo(aiPolyMeshSample* sample, aiSplitedMeshData *o_smi, const aiSplitedMeshData *prev, int max_vertices)
{
    if (!sample) { return true; }
    return sample->getSplitedMeshInfo(*o_smi, *prev, max_vertices);
}

aiCLinkage aiExport void aiPolyMeshCopySplitedMesh(aiPolyMeshSample* sample, aiSplitedMeshData *o_smi)
{
    if (!sample) { return; }
    return sample->copySplitedMesh(*o_smi);
}

aiCLinkage aiExport void aiPolyMeshCopyToTexture(aiPolyMeshSample* sample, aiTextureMeshData *dst)
{
#ifdef aiSupportTextureMesh
    if (!sample) { return; }
    sample->copyMeshToTexture(*dst);
#endif // aiSupportTextureMesh
}



aiCLinkage aiExport bool aiHasCamera(aiObject* obj)
{
    return obj->hasCamera();
}

aiCLinkage aiExport aiCamera* aiGetCamera(aiObject* obj)
{
    return &obj->getCamera();
}

aiCLinkage aiExport void aiCameraGetData(aiCameraSample* sample, aiCameraData *o_params)
{
    if (!sample) { return; }
    sample->getParams(*o_params);
}
