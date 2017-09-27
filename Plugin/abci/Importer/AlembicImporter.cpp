#include "pch.h"
#include "aiInternal.h"
#include "aiContext.h"
#include "aiObject.h"
#include "aiSchema.h"
#include "aiXForm.h"
#include "aiPolyMesh.h"
#include "aiCamera.h"
#include "aiPoints.h"
#include "aiProperty.h"

abciAPI abcSampleSelector aiTimeToSampleSelector(float time)
{
    return abcSampleSelector(static_cast<AbcCoreAbstract::chrono_t>(time), Abc::ISampleSelector::kFloorIndex);
}

abciAPI abcSampleSelector aiIndexToSampleSelector(int64_t index)
{
    return abcSampleSelector(index);
}

abciAPI void aiEnableFileLog(bool on, const char *path)
{
    aiLogger::Enable(on, path);
}

abciAPI void aiCleanup()
{
#ifdef aiWithTBB
#else
    aiThreadPool::releaseInstance();
#endif
}

abciAPI void clearContextsWithPath(const char *path)
{
    aiContextManager::destroyContextsWithPath(path);
}

abciAPI aiContext* aiCreateContext(int uid)
{
    return aiContextManager::getContext(uid);
}

abciAPI void aiDestroyContext(aiContext* ctx)
{
    if (ctx)
    {
        aiContextManager::destroyContext(ctx->getUid());
    }
}


abciAPI bool aiLoad(aiContext* ctx, const char *path)
{
    return ctx ? ctx->load(path) : false;
}

abciAPI void aiSetConfig(aiContext* ctx, const aiConfig* conf)
{
    if (ctx)
    {
        ctx->setConfig(*conf);
    }
}

abciAPI float aiGetStartTime(aiContext* ctx)
{
    return (ctx ? ctx->getStartTime() : 0.0f);
}

abciAPI float aiGetEndTime(aiContext* ctx)
{
    return (ctx ? ctx->getEndTime() : 0.0f);
}

abciAPI aiObject* aiGetTopObject(aiContext* ctx)
{
    return (ctx ? ctx->getTopObject() : 0);
}

abciAPI void aiDestroyObject(aiContext* ctx, aiObject* obj)
{
    if (ctx)
    {
        ctx->destroyObject(obj);
    }
}

abciAPI int aiGetNumTimeSamplings(aiContext* ctx)
{
    if (ctx)
    {
        return ctx->getNumTimeSamplings();
    }
    return 0;
}
abciAPI void aiGetTimeSampling(aiContext* ctx, int i, aiTimeSamplingData *dst)
{
    return ctx->getTimeSampling(i, *dst);
}
abciAPI void aiCopyTimeSampling(aiContext* ctx, int i, aiTimeSamplingData *dst)
{
    return ctx->copyTimeSampling(i, *dst);
}

abciAPI void aiUpdateSamples(aiContext* ctx, float time)
{
    if (ctx)
    {
        ctx->updateSamples(time);
    }
}

abciAPI void aiEnumerateChild(aiObject *obj, aiNodeEnumerator e, void *userData)
{
    if (obj == nullptr) { return; }

    try
    {
        obj->eachChildren([&](aiObject *child) { e(child, userData); });
    }
    catch (Alembic::Util::Exception ex)
    {
        DebugLog("aiEnumerateChlid: %s", ex.what());
    }
}

abciAPI const char* aiGetNameS(aiObject* obj)
{
    return (obj ? obj->getName() : "");
}

abciAPI const char* aiGetFullNameS(aiObject* obj)
{
    return (obj ? obj->getFullName() : "");
}

abciAPI int aiGetNumChildren(aiObject* obj)
{
    return (obj ? obj->getNumChildren() : 0);
}

abciAPI aiObject* aiGetChild(aiObject* obj, int i)
{
    return (obj ? obj->getChild(i) : nullptr);
}

abciAPI aiObject* aiGetParent(aiObject* obj)
{
    return (obj ? obj->getParent() : nullptr);
}

abciAPI void aiSchemaSetSampleCallback(aiSchemaBase* schema, aiSampleCallback cb, void* arg)
{
    if (schema)
    {
        schema->setSampleCallback(cb, arg);
    }
}

abciAPI void aiSchemaSetConfigCallback(aiSchemaBase* schema, aiConfigCallback cb, void* arg)
{
    if (schema)
    {
        schema->setConfigCallback(cb, arg);
    }
}

abciAPI aiObject* aiSchemaGetObject(aiSchemaBase* schema)
{
    return (schema ? schema->getObject() : nullptr);
}

abciAPI int aiSchemaGetNumSamples(aiSchemaBase* schema)
{
    return (schema ? schema->getNumSamples() : 0);
}

abciAPI aiSampleBase* aiSchemaUpdateSample(aiSchemaBase* schema, const abcSampleSelector *ss)
{    
    return (schema ? schema->updateSample(*ss) : 0);
}

abciAPI aiSampleBase* aiSchemaGetSample(aiSchemaBase* schema, const abcSampleSelector *ss)
{
    return (schema ? schema->getSample(*ss) : 0);
}

abciAPI int aiSchemaGetSampleIndex(aiSchemaBase* schema, const abcSampleSelector *ss)
{
    return schema ? schema->getSampleIndex(*ss) : 0;
}

abciAPI float aiSchemaGetSampleTime(aiSchemaBase* schema, const abcSampleSelector *ss)
{
    return schema ? schema->getSampleTime(*ss) : 0.0f;
}

abciAPI int aiSchemaGetTimeSamplingIndex(aiSchemaBase* schema)
{
    return schema ? schema->getTimeSamplingIndex() : 0;
}


abciAPI bool aiHasXForm(aiObject* obj)
{
    return obj && obj->getXForm();
}

abciAPI aiXForm* aiGetXForm(aiObject* obj)
{
    return obj ? obj->getXForm() : nullptr;
}

abciAPI void aiXFormGetData(aiXFormSample* sample, aiXFormData *outData)
{
    if (sample)
    {
        sample->getData(*outData);
    }
}

abciAPI bool aiHasPolyMesh(aiObject* obj)
{
    return obj && obj->getPolyMesh();
}

abciAPI aiPolyMesh* aiGetPolyMesh(aiObject* obj)
{
    return obj ? obj->getPolyMesh() : nullptr;
}

abciAPI void aiPolyMeshGetSummary(aiPolyMesh* schema, aiMeshSummary* summary)
{
    if (schema)
    {
        schema->getSummary(*summary);
    }
}

abciAPI void aiPolyMeshGetSampleSummary(aiPolyMeshSample* sample, aiMeshSampleSummary* summary, bool forceRefresh)
{
    if (sample)
    {
        sample->getSummary(forceRefresh, *summary, sample);
    }
}

abciAPI void aiPolyMeshGetDataPointer(aiPolyMeshSample* sample, aiPolyMeshData* data)
{
    if (sample)
    {
        sample->getDataPointer(*data);
    }
}

abciAPI void aiPolyMeshCopyData(aiPolyMeshSample* sample, aiPolyMeshData* data, int triangulate, int always_expand_indices)
{
    if (sample)
    {
        if (triangulate != 0) {
            sample->copyDataWithTriangulation(*data, always_expand_indices != 0);
        }
        else {
            sample->copyData(*data);
        }
    }
}

abciAPI int aiPolyMeshGetVertexBufferLength(aiPolyMeshSample* sample, int splitIndex)
{
    return (sample ? sample->getVertexBufferLength(splitIndex) : 0);
}

abciAPI void aiPolyMeshFillVertexBuffer(aiPolyMeshSample* sample, int splitIndex, aiPolyMeshData* data)
{
    if (sample)
    {
        sample->fillVertexBuffer(splitIndex, *data);
    }
}

abciAPI int aiPolyMeshPrepareSubmeshes(aiPolyMeshSample* sample, const aiFacesets* facesets)
{
    return (sample ? sample->prepareSubmeshes(sample, *facesets) : 0);
}

abciAPI int aiPolyMeshGetSplitSubmeshCount(aiPolyMeshSample* sample, int splitIndex)
{
    return (sample ? sample->getSplitSubmeshCount(splitIndex) : 0);
}

abciAPI bool aiPolyMeshGetNextSubmesh(aiPolyMeshSample* sample, aiSubmeshSummary* summary)
{
    return sample ? sample->getNextSubmesh(*summary) : false;
}

abciAPI void aiPolyMeshFillSubmeshIndices(aiPolyMeshSample* sample, const aiSubmeshSummary* summary, aiSubmeshData* data)
{
    if (sample)
    {
        sample->fillSubmeshIndices(*summary, *data);
    }
}

abciAPI bool aiHasCamera(aiObject* obj)
{
    return obj && obj->getCamera();
}

abciAPI aiCamera* aiGetCamera(aiObject* obj)
{
    return obj ? obj->getCamera() : nullptr;
}

abciAPI void aiCameraGetData(aiCameraSample* sample, aiCameraData *outData)
{
    if (sample)
    {
        sample->getData(*outData);
    }
}

abciAPI bool aiHasPoints(aiObject* obj)
{
    return obj && obj->getPoints();
}

abciAPI aiPoints* aiGetPoints(aiObject* obj)
{
    return obj ? obj->getPoints() : nullptr;
}

abciAPI void aiPointsGetSummary(aiPoints *schema, aiPointsSummary *summary)
{
    if (schema == nullptr) { return; }
    *summary = schema->getSummary();
}
abciAPI void aiPointsSetSort(aiPoints* schema, bool v)
{
    if (schema == nullptr) { return; }
    schema->setSort(v);
}
abciAPI void aiPointsSetSortBasePosition(aiPoints* schema, abcV3 v)
{
    if (schema == nullptr) { return; }
    schema->setSortPosition(v);
}

abciAPI void aiPointsGetDataPointer(aiPointsSample* sample, aiPointsData *outData)
{
    if (sample == nullptr) { return; }
    sample->getDataPointer(*outData);
}

abciAPI void aiPointsCopyData(aiPointsSample* sample, aiPointsData *outData)
{
    if (sample == nullptr) { return; }
    sample->copyData(*outData);
}


abciAPI int aiSchemaGetNumProperties(aiSchemaBase* schema)
{
    return schema->getNumProperties();
}

abciAPI aiProperty* aiSchemaGetPropertyByIndex(aiSchemaBase* schema, int i)
{
    return schema->getPropertyByIndex(i);
}

abciAPI aiProperty* aiSchemaGetPropertyByName(aiSchemaBase* schema, const char *name)
{
    return schema->getPropertyByName(name);
}

abciAPI aiPropertyType aiPropertyGetType(aiProperty* prop)
{
    return prop->getPropertyType();
}

abciAPI int aiPropertyGetTimeSamplingIndex(aiProperty* prop)
{
    return prop->getTimeSamplingIndex();
}

abciAPI const char* aiPropertyGetNameS(aiProperty* prop)
{
    return prop->getName().c_str();
}

abciAPI void aiPropertyGetDataPointer(aiProperty* prop, const abcSampleSelector *ss, aiPropertyData *data)
{
    prop->getDataPointer(*ss, *data);
}
abciAPI void aiPropertyCopyData(aiProperty* prop, const abcSampleSelector *ss, aiPropertyData *data)
{
    prop->copyData(*ss, *data);
}