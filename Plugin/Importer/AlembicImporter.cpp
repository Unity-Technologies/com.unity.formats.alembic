#include "pch.h"
#include "AlembicImporter.h"
#include "aiLogger.h"
#include "aiContext.h"
#include "aiObject.h"
#include "aiSchema.h"
#include "aiXForm.h"
#include "aiPolyMesh.h"
#include "aiCamera.h"
#include "aiPoints.h"
#include "aiProperty.h"

#ifdef aiWindows
    #include <windows.h>

#   define aiBreak() DebugBreak()
#else // aiWindows
#   define aiBreak() __builtin_trap()
#endif // aiWindows

aiCLinkage aiExport abcSampleSelector aiTimeToSampleSelector(float time)
{
    return abcSampleSelector(double(time), Abc::ISampleSelector::kFloorIndex);
}

aiCLinkage aiExport abcSampleSelector aiIndexToSampleSelector(int index)
{
    return abcSampleSelector(int64_t(index));
}


aiCLinkage aiExport void aiEnableFileLog(bool on, const char *path)
{
    aiLogger::Enable(on, path);
}

aiCLinkage aiExport void aiCleanup()
{
#ifdef aiWithTBB
#else
    aiThreadPool::releaseInstance();
#endif
}

aiCLinkage aiExport aiContext* aiCreateContext(int uid)
{
    auto ctx = aiContext::create(uid);
    return ctx;
}

aiCLinkage aiExport void aiDestroyContext(aiContext* ctx)
{
    if (ctx)
    {
        aiContext::destroy(ctx);
    }
}


aiCLinkage aiExport bool aiLoad(aiContext* ctx, const char *path)
{
    return (ctx ? ctx->load(path) : false);
}

aiCLinkage aiExport void aiSetConfig(aiContext* ctx, const aiConfig* conf)
{
    if (ctx)
    {
        ctx->setConfig(*conf);
    }
}

aiCLinkage aiExport float aiGetStartTime(aiContext* ctx)
{
    return (ctx ? ctx->getStartTime() : 0.0f);
}

aiCLinkage aiExport float aiGetEndTime(aiContext* ctx)
{
    return (ctx ? ctx->getEndTime() : 0.0f);
}

aiCLinkage aiExport aiObject* aiGetTopObject(aiContext* ctx)
{
    return (ctx ? ctx->getTopObject() : 0);
}

aiCLinkage aiExport void aiDestroyObject(aiContext* ctx, aiObject* obj)
{
    if (ctx)
    {
        ctx->destroyObject(obj);
    }
}

aiCLinkage aiExport int aiGetNumTimeSamplings(aiContext* ctx)
{
    if (ctx)
    {
        return ctx->getNumTimeSamplings();
    }
    return 0;
}
aiCLinkage aiExport void aiGetTimeSampling(aiContext* ctx, int i, aiTimeSamplingData *dst)
{
    return ctx->getTimeSampling(i, *dst);
}
aiCLinkage aiExport void aiCopyTimeSampling(aiContext* ctx, int i, aiTimeSamplingData *dst)
{
    return ctx->copyTimeSampling(i, *dst);
}

aiCLinkage aiExport void aiUpdateSamples(aiContext* ctx, float time)
{
    if (ctx)
    {
        ctx->updateSamples(time);
    }
}
aiCLinkage aiExport void aiUpdateSamplesBegin(aiContext* ctx, float time)
{
    if (ctx)
    {
        ctx->updateSamplesBegin(time);
    }
}
aiCLinkage aiExport void aiUpdateSamplesEnd(aiContext* ctx)
{
    if (ctx)
    {
        ctx->updateSamplesEnd();
    }
}

aiCLinkage aiExport void aiEnumerateChild(aiObject *obj, aiNodeEnumerator e, void *userData)
{
    if (obj == nullptr) { return; }

    try
    {
        obj->eachChildren([&](aiObject *child) { e(child, userData); });
    }
    catch (Alembic::Util::Exception e)
    {
        DebugLog("aiEnumerateChlid: %s", e.what());
    }
}

aiCLinkage aiExport const char* aiGetNameS(aiObject* obj)
{
    return (obj ? obj->getName() : "");
}

aiCLinkage aiExport const char* aiGetFullNameS(aiObject* obj)
{
    return (obj ? obj->getFullName() : "");
}

aiCLinkage aiExport int aiGetNumChildren(aiObject* obj)
{
    return (obj ? obj->getNumChildren() : 0);
}

aiCLinkage aiExport aiObject* aiGetChild(aiObject* obj, int i)
{
    return (obj ? obj->getChild(i) : nullptr);
}

aiCLinkage aiExport aiObject* aiGetParent(aiObject* obj)
{
    return (obj ? obj->getParent() : nullptr);
}


aiCLinkage aiExport void aiSchemaSetSampleCallback(aiSchemaBase* schema, aiSampleCallback cb, void* arg)
{
    if (schema)
    {
        schema->setSampleCallback(cb, arg);
    }
}

aiCLinkage aiExport void aiSchemaSetConfigCallback(aiSchemaBase* schema, aiConfigCallback cb, void* arg)
{
    if (schema)
    {
        schema->setConfigCallback(cb, arg);
    }
}

aiCLinkage aiExport int aiSchemaGetNumSamples(aiSchemaBase* schema)
{
    return (schema ? schema->getNumSamples() : 0);
}

aiCLinkage aiExport aiSampleBase* aiSchemaUpdateSample(aiSchemaBase* schema, const abcSampleSelector *ss)
{    
    return (schema ? schema->updateSample(*ss) : 0);
}

aiCLinkage aiExport aiSampleBase* aiSchemaGetSample(aiSchemaBase* schema, const abcSampleSelector *ss)
{
    return (schema ? schema->getSample(*ss) : 0);
}

aiCLinkage aiExport int aiSchemaGetSampleIndex(aiSchemaBase* schema, const abcSampleSelector *ss)
{
    return schema ? schema->getSampleIndex(*ss) : 0;
}

aiCLinkage aiExport float aiSchemaGetSampleTime(aiSchemaBase* schema, const abcSampleSelector *ss)
{
    return schema ? schema->getSampleTime(*ss) : 0.0f;
}

aiCLinkage aiExport int aiSchemaGetTimeSamplingIndex(aiSchemaBase* schema)
{
    return schema ? schema->getTimeSamplingIndex() : 0;
}


aiCLinkage aiExport bool aiHasXForm(aiObject* obj)
{
    return obj && obj->getXForm();
}

aiCLinkage aiExport aiXForm* aiGetXForm(aiObject* obj)
{
    return obj ? obj->getXForm() : nullptr;
}

aiCLinkage aiExport void aiXFormGetData(aiXFormSample* sample, aiXFormData *outData)
{
    if (sample)
    {
        sample->getData(*outData);
    }
}


aiCLinkage aiExport bool aiHasPolyMesh(aiObject* obj)
{
    return obj && obj->getPolyMesh();
}

aiCLinkage aiExport aiPolyMesh* aiGetPolyMesh(aiObject* obj)
{
    return obj ? obj->getPolyMesh() : nullptr;
}

aiCLinkage aiExport void aiPolyMeshGetSummary(aiPolyMesh* schema, aiMeshSummary* summary)
{
    if (schema)
    {
        schema->getSummary(*summary);
    }
}

aiCLinkage aiExport void aiPolyMeshGetSampleSummary(aiPolyMeshSample* sample, aiMeshSampleSummary* summary, bool forceRefresh)
{
    if (sample)
    {
        sample->getSummary(forceRefresh, *summary);
    }
}

aiCLinkage aiExport void aiPolyMeshGetDataPointer(aiPolyMeshSample* sample, aiPolyMeshData* data)
{
    if (sample)
    {
        sample->getDataPointer(*data);
    }
}

aiCLinkage aiExport void aiPolyMeshCopyData(aiPolyMeshSample* sample, aiPolyMeshData* data, bool triangulate, bool always_expand_indices)
{
    if (sample)
    {
        if (triangulate) {
            sample->copyDataWithTriangulation(*data, always_expand_indices);
        }
        else {
            sample->copyData(*data);
        }
    }
}


aiCLinkage aiExport int aiPolyMeshGetVertexBufferLength(aiPolyMeshSample* sample, int splitIndex)
{
    return (sample ? sample->getVertexBufferLength(splitIndex) : 0);
}

aiCLinkage aiExport void aiPolyMeshFillVertexBuffer(aiPolyMeshSample* sample, int splitIndex, aiPolyMeshData* data)
{
    if (sample)
    {
        sample->fillVertexBuffer(splitIndex, *data);
    }
}

aiCLinkage aiExport int aiPolyMeshPrepareSubmeshes(aiPolyMeshSample* sample, const aiFacesets* facesets)
{
    return (sample ? sample->prepareSubmeshes(*facesets) : 0);
}

aiCLinkage aiExport int aiPolyMeshGetSplitSubmeshCount(aiPolyMeshSample* sample, int splitIndex)
{
    return (sample ? sample->getSplitSubmeshCount(splitIndex) : 0);
}

aiCLinkage aiExport bool aiPolyMeshGetNextSubmesh(aiPolyMeshSample* sample, aiSubmeshSummary* summary)
{
    return (sample ? sample->getNextSubmesh(*summary) : false);
}

aiCLinkage aiExport void aiPolyMeshFillSubmeshIndices(aiPolyMeshSample* sample, const aiSubmeshSummary* summary, aiSubmeshData* data)
{
    if (sample)
    {
        sample->fillSubmeshIndices(*summary, *data);
    }
}


aiCLinkage aiExport bool aiHasCamera(aiObject* obj)
{
    return obj && obj->getCamera();
}

aiCLinkage aiExport aiCamera* aiGetCamera(aiObject* obj)
{
    return obj ? obj->getCamera() : nullptr;
}

aiCLinkage aiExport void aiCameraGetData(aiCameraSample* sample, aiCameraData *outData)
{
    if (sample)
    {
        sample->getData(*outData);
    }
}

aiCLinkage aiExport bool aiHasPoints(aiObject* obj)
{
    return obj && obj->getPoints();
}

aiCLinkage aiExport aiPoints* aiGetPoints(aiObject* obj)
{
    return obj ? obj->getPoints() : nullptr;
}

aiCLinkage aiExport int aiPointsGetPeakVertexCount(aiPoints *schema)
{
    if (schema == nullptr) { return 0; }
    return schema->getPeakVertexCount();
}

aiCLinkage aiExport void aiPointsGetDataPointer(aiPointsSample* sample, aiPointsData *outData)
{
    if (sample == nullptr) { return; }
    sample->getDataPointer(*outData);
}

aiCLinkage aiExport void aiPointsCopyData(aiPointsSample* sample, aiPointsData *outData)
{
    if (sample == nullptr) { return; }
    sample->copyData(*outData);
}


aiCLinkage aiExport int aiSchemaGetNumProperties(aiSchemaBase* schema)
{
    return schema->getNumProperties();
}

aiCLinkage aiExport aiProperty* aiSchemaGetPropertyByIndex(aiSchemaBase* schema, int i)
{
    return schema->getPropertyByIndex(i);
}

aiCLinkage aiExport aiProperty* aiSchemaGetPropertyByName(aiSchemaBase* schema, const char *name)
{
    return schema->getPropertyByName(name);
}

aiCLinkage aiExport aiPropertyType aiPropertyGetType(aiProperty* prop)
{
    return prop->getPropertyType();
}

aiCLinkage aiExport int aiPropertyGetTimeSamplingIndex(aiProperty* prop)
{
    return prop->getTimeSamplingIndex();
}

aiCLinkage aiExport const char* aiPropertyGetNameS(aiProperty* prop)
{
    return prop->getName().c_str();
}

aiCLinkage aiExport void aiPropertyGetDataPointer(aiProperty* prop, const abcSampleSelector *ss, aiPropertyData *data)
{
    prop->getDataPointer(*ss, *data);
}
aiCLinkage aiExport void aiPropertyCopyData(aiProperty* prop, const abcSampleSelector *ss, aiPropertyData *data)
{
    prop->copyData(*ss, *data);
}


#ifdef aiSupportTexture

#include "GraphicsDevice/aiGraphicsDevice.h"

aiCLinkage aiExport bool aiPointsCopyPositionsToTexture(aiPointsData *data, void *tex, int width, int height, aiTextureFormat fmt)
{
    if (data == nullptr) { return false; }

    if (fmt == aiTextureFormat_ARGBFloat)
    {
        return aiWriteTextureWithConversion(tex, width, height, fmt, data->positions, data->count,
            [](void *dst, const abcV3 *src, int i) {
                ((abcV4*)dst)[i] = abcV4(src[i].x, src[i].y, src[i].z, 0.0f);
            });
    }
    else {
        DebugLog("aiPointsCopyPositionsToTexture(): format must be ARGBFloat");
        return false;
    }
}

aiCLinkage aiExport bool aiPointsCopyIDsToTexture(aiPointsData *data, void *tex, int width, int height, aiTextureFormat fmt)
{
    if (data == nullptr) { return false; }

    if (fmt == aiTextureFormat_RInt)
    {
        return aiWriteTextureWithConversion(tex, width, height, fmt, data->ids, data->count,
            [](void *dst, const uint64_t *src, int i) {
                ((int32_t*)dst)[i] = (int32_t)(src[i]);
            });
    }
    else if (fmt == aiTextureFormat_RFloat)
    {
        return aiWriteTextureWithConversion(tex, width, height, fmt, data->ids, data->count,
            [](void *dst, const uint64_t *src, int i) {
                ((float*)dst)[i] = (float)(src[i]);
            });
    }
    else
    {
        DebugLog("aiPointsCopyIDsToTexture(): format must be RFloat or RInt");
        return false;
    }
}

#endif // aiSupportTexture
