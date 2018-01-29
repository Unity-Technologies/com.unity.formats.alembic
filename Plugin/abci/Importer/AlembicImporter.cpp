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
        aiContextManager::destroyContext(ctx->getUid());
}


abciAPI bool aiLoad(aiContext* ctx, const char *path)
{
    return ctx ? ctx->load(path) : false;
}

abciAPI void aiSetConfig(aiContext* ctx, const aiConfig* conf)
{
    if (ctx)
        ctx->setConfig(*conf);
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

abciAPI void aiUpdateSamples(aiContext* ctx, float time)
{
    if (ctx)
        ctx->updateSamples(time);
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

abciAPI void aiSetEnabled(aiObject * obj, bool v)
{
    if (obj)
        obj->setEnabled(v);
}

abciAPI aiObject* aiGetParent(aiObject* obj)
{
    return obj ? obj->getParent() : nullptr;
}

abciAPI aiObject* aiSchemaGetObject(aiSchemaBase* schema)
{
    return schema ? schema->getObject() : nullptr;
}

abciAPI void aiSchemaSync(aiSchemaBase* schema)
{
    if (schema)
        schema->sync();
}

abciAPI bool aiSchemaIsConstant(aiSchemaBase * schema)
{
    return schema ? schema->isConstant() : false;
}

abciAPI bool aiSchemaIsDataUpdated(aiSchemaBase* schema)
{
    return schema ? schema->isDataUpdated() : false;
}

abciAPI void aiSchemaMarkForceUpdate(aiSchemaBase * schema)
{
    if (schema)
        schema->markForceUpdate();
}

abciAPI void aiSampleSync(aiSampleBase * sample)
{
    if (sample)
        sample->sync();
}


abciAPI aiSampleBase * aiSchemaGetSample(aiSchemaBase * schema)
{
    return schema ? schema->getSample() : nullptr;
}

abciAPI void aiSchemaUpdateSample(aiSchemaBase* schema, const abcSampleSelector *ss)
{
    if (schema) {
        schema->markForceSync();
        schema->updateSample(*ss);
    }
}


abciAPI aiXform* aiGetXform(aiObject* obj)
{
    return obj ? obj->getXform() : nullptr;
}

abciAPI void aiXformGetData(aiXformSample* sample, aiXformData *dst)
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

abciAPI void aiPolyMeshGetSplitSummaries(aiPolyMeshSample* sample, aiMeshSplitSummary *dst)
{
    if (sample)
        sample->getSplitSummaries(dst);
}

abciAPI void aiPolyMeshGetSubmeshSummaries(aiPolyMeshSample* sample, aiSubmeshSummary* dst)
{
    if (sample)
        sample->getSubmeshSummaries(dst);
}

abciAPI void aiPolyMeshFillVertexBuffer(aiPolyMeshSample* sample, aiPolyMeshData* vbs, aiSubmeshData* ibs)
{
    if (sample)
        sample->fillVertexBuffer(vbs, ibs);
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