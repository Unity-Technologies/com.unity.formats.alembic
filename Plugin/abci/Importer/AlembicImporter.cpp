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
}

abciAPI void aiClearContextsWithPath(const char *path)
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
    return ctx ? ctx->getStartTime() : 0.0f;
}

abciAPI float aiGetEndTime(aiContext* ctx)
{
    return ctx ? ctx->getEndTime() : 0.0f;
}

abciAPI int aiGetFrameCount(aiContext* ctx)
{
    return ctx ? ctx->getFrameCount() : 0;
}

abciAPI aiObject* aiGetTopObject(aiContext* ctx)
{
    return ctx ? ctx->getTopObject() : 0;
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
    return ctx ? ctx->getNumTimeSamplings() : 0;
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

abciAPI void aiEnumerateChild(aiObject *obj, aiNodeEnumerator e, void *user_data)
{
    if (obj == nullptr) { return; }

    try
    {
        obj->eachChildren([&](aiObject *child) { e(child, user_data); });
    }
    catch (Alembic::Util::Exception ex)
    {
        DebugLog("aiEnumerateChlid: %s", ex.what());
    }
}

abciAPI const char* aiGetNameS(aiObject* obj)
{
    return obj ? obj->getName() : "";
}

abciAPI const char* aiGetFullNameS(aiObject* obj)
{
    return obj ? obj->getFullName() : "";
}

abciAPI int aiGetNumChildren(aiObject* obj)
{
    return obj ? obj->getNumChildren() : 0;
}

abciAPI aiObject* aiGetChild(aiObject* obj, int i)
{
    return obj ? obj->getChild(i) : nullptr;
}

abciAPI aiObject* aiGetParent(aiObject* obj)
{
    return obj ? obj->getParent() : nullptr;
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
    return schema ? schema->getObject() : nullptr;
}

abciAPI int aiSchemaGetNumSamples(aiSchemaBase* schema)
{
    return schema ? schema->getNumSamples() : 0;
}

abciAPI aiSampleBase* aiSchemaUpdateSample(aiSchemaBase* schema, const abcSampleSelector *ss)
{    
    return schema ? schema->updateSample(*ss) : 0;
}

abciAPI aiSampleBase* aiSchemaGetSample(aiSchemaBase* schema, const abcSampleSelector *ss)
{
    return schema ? schema->getSample(*ss) : 0;
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


abciAPI aiXForm* aiGetXForm(aiObject* obj)
{
    return obj ? obj->getXForm() : nullptr;
}

abciAPI void aiXFormGetData(aiXFormSample* sample, aiXFormData *dst)
{
    if (sample)
    {
        sample->getData(*dst);
    }
}

abciAPI aiPolyMesh* aiGetPolyMesh(aiObject* obj)
{
    return obj ? obj->getPolyMesh() : nullptr;
}

abciAPI void aiPolyMeshGetSummary(aiPolyMesh* schema, aiMeshSummary* dst)
{
    if (schema)
        schema->getSummary(*dst);
}

abciAPI void aiPolyMeshGetSampleSummary(aiPolyMeshSample* sample, aiMeshSampleSummary* dst)
{
    if (sample)
        sample->getSummary(*dst);
}

abciAPI void aiPolyMeshGetSplitSummary(aiPolyMeshSample* sample, int split_index, aiMeshSplitSummary *dst)
{
    if (sample)
        sample->getSplitSummary(split_index, *dst);
}

abciAPI void aiPolyMeshGetSubmeshSummary(aiPolyMeshSample* sample, int split_index, int submesh_index, aiSubmeshSummary* dst)
{
    if (sample)
        sample->getSubmeshSummary(split_index, submesh_index, *dst);
}

abciAPI void aiPolyMeshPrepareSplits(aiPolyMeshSample * sample)
{
    if (sample)
        sample->prepareSplits();
}

abciAPI void aiPolyMeshFillVertexBuffer(aiPolyMeshSample* sample, int split_index, aiPolyMeshData* dst)
{
    if (sample)
        sample->fillSplitVertices(split_index, *dst);
}

abciAPI void aiPolyMeshFillSubmeshIndices(aiPolyMeshSample* sample, int split_index, int submesh_index, aiSubmeshData* dst)
{
    if (sample)
        sample->fillSubmeshIndices(split_index, submesh_index, *dst);
}

abciAPI aiCamera* aiGetCamera(aiObject* obj)
{
    return obj ? obj->getCamera() : nullptr;
}

abciAPI void aiCameraGetData(aiCameraSample* sample, aiCameraData *dst)
{
    if (sample)
    {
        sample->getData(*dst);
    }
}

abciAPI aiPoints* aiGetPoints(aiObject* obj)
{
    return obj ? obj->getPoints() : nullptr;
}

abciAPI void aiPointsGetSummary(aiPoints *schema, aiPointsSummary *dst)
{
    if (schema == nullptr) { return; }
    *dst = schema->getSummary();
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

abciAPI void aiPointsGetDataPointer(aiPointsSample* sample, aiPointsData *dst)
{
    if (sample == nullptr) { return; }
    sample->getDataPointer(*dst);
}

abciAPI void aiPointsCopyData(aiPointsSample* sample, aiPointsData *dst)
{
    if (sample == nullptr) { return; }
    sample->copyData(*dst);
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
abciAPI void aiPropertyCopyData(aiProperty* prop, const abcSampleSelector *ss, aiPropertyData *dst)
{
    prop->copyData(*ss, *dst);
}