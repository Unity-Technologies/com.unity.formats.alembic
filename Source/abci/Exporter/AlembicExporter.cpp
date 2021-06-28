#include "pch.h"
#include "aeInternal.h"
#include "aeContext.h"
#include "aeObject.h"
#include "aeXForm.h"
#include "aePoints.h"
#include "aePolyMesh.h"
#include "aeCamera.h"
#include "../Foundation/aiMeshOps.h"


abciAPI aeContext* aeCreateContext()
{
    return new aeContext();
}

abciAPI void aeDestroyContext(aeContext* ctx)
{
    delete ctx;
}

abciAPI void aeSetConfig(aeContext* ctx, const aeConfig *conf)
{
    if (ctx)
        ctx->setConfig(*conf);
}

abciAPI bool aeOpenArchive(aeContext* ctx, const char *path)
{
    return ctx ? ctx->openArchive(path) : false;
}

abciAPI aeObject* aeGetTopObject(aeContext* ctx)
{
    return ctx ? ctx->getTopObject() : nullptr;
}

abciAPI int aeAddTimeSampling(aeContext* ctx, float start_time)
{
    return ctx ? ctx->addTimeSampling(start_time) : 0;
}

abciAPI void aeAddTime(aeContext* ctx, float time, int tsi)
{
    if (ctx)
        ctx->addTime(time, tsi);
}

abciAPI void aeMarkFrameBegin(aeContext* ctx)
{
    if (ctx)
        ctx->markFrameBegin();
}

abciAPI void aeMarkFrameEnd(aeContext* ctx)
{
    if (ctx)
        ctx->markFrameEnd();
}

abciAPI void aeDeleteObject(aeObject *obj)
{
    delete obj;
}

abciAPI aeXform* aeNewXform(aeObject *obj, const char *name, int tsi)
{
    return obj ? obj->newChild<aeXform>(name, tsi) : nullptr;
}

abciAPI aePoints* aeNewPoints(aeObject *obj, const char *name, int tsi)
{
    return obj ? obj->newChild<aePoints>(name, tsi) : nullptr;
}

abciAPI aePolyMesh* aeNewPolyMesh(aeObject *obj, const char *name, int tsi)
{
    return obj ? obj->newChild<aePolyMesh>(name, tsi) : nullptr;
}

abciAPI aeCamera* aeNewCamera(aeObject *obj, const char *name, int tsi)
{
    return obj ? obj->newChild<aeCamera>(name, tsi) : nullptr;
}

abciAPI int aeGetNumChildren(aeObject *obj)
{
    return obj ? (int)obj->getNumChildren() : 0;
}

abciAPI aeObject* aeGetChild(aeObject *obj, int i)
{
    return obj ? obj->getChild(i) : nullptr;
}

abciAPI aeObject* aeGetParent(aeObject *obj)
{
    return obj ? obj->getParent() : nullptr;
}

abciAPI aeXform* aeAsXform(aeObject *obj)
{
    return dynamic_cast<aeXform*>(obj);
}

abciAPI aePoints* aeAsPoints(aeObject *obj)
{
    return dynamic_cast<aePoints*>(obj);
}

abciAPI aePolyMesh* aeAsPolyMesh(aeObject *obj)
{
    return dynamic_cast<aePolyMesh*>(obj);
}

abciAPI aeCamera* aeAsCamera(aeObject *obj)
{
    return dynamic_cast<aeCamera*>(obj);
}

abciAPI int aeGetNumSamples(aeSchema *obj)
{
    return obj ? (int)obj->getNumSamples() : 0;
}

abciAPI void aeSetFromPrevious(aeSchema *obj)
{
    if (obj)
        obj->setFromPrevious();
}

abciAPI void aeMarkForceInvisible(aeSchema * obj)
{
    if (obj)
        obj->markForceInvisible();
}

abciAPI void aeXformWriteSample(aeXform *obj, const aeXformData *data)
{
    if (obj)
        obj->writeSample(*data);
}

abciAPI void aeCameraWriteSample(aeCamera *obj, const CameraData *data)
{
    if (obj)
        obj->writeSample(*data);
}

abciAPI void aePointsWriteSample(aePoints *obj, const aePointsData *data)
{
    if (obj)
        obj->writeSample(*data);
}

abciAPI int aePolyMeshAddFaceSet(aePolyMesh *obj, const char *name)
{
    return obj ? obj->addFaceSet(name) : 0;
}

abciAPI void aePolyMeshWriteSample(aePolyMesh *obj, const aePolyMeshData *data)
{
    if (obj)
        obj->writeSample(*data);
}

abciAPI void aePolyMeshWriteFaceSetSample(aePolyMesh *obj, int fsi, const aeFaceSetData *data)
{
    if (obj)
        obj->writeFaceSetSample(fsi, *data);
}

abciAPI aeProperty* aeNewProperty(aeSchema *obj, const char *name, aePropertyType type)
{
    if (!obj)
        return nullptr;

    switch (type)
    {
        // scalar properties
        case aePropertyType::Bool:           return obj->newProperty<abcBoolProperty>(name); break;
        case aePropertyType::Int:            return obj->newProperty<abcIntProperty>(name); break;
        case aePropertyType::UInt:           return obj->newProperty<abcUIntProperty>(name); break;
        case aePropertyType::Float:          return obj->newProperty<abcFloatProperty>(name); break;
        case aePropertyType::Float2:         return obj->newProperty<abcFloat2Property>(name); break;
        case aePropertyType::Float3:         return obj->newProperty<abcFloat3Property>(name); break;
        case aePropertyType::Float4:         return obj->newProperty<abcFloat4Property>(name); break;
        case aePropertyType::Float4x4:       return obj->newProperty<abcFloat4x4Property>(name); break;

        // array properties
        case aePropertyType::BoolArray:      return obj->newProperty<abcBoolArrayProperty>(name); break;
        case aePropertyType::IntArray:       return obj->newProperty<abcIntArrayProperty>(name); break;
        case aePropertyType::UIntArray:      return obj->newProperty<abcUIntArrayProperty>(name); break;
        case aePropertyType::FloatArray:     return obj->newProperty<abcFloatArrayProperty>(name); break;
        case aePropertyType::Float2Array:    return obj->newProperty<abcFloat2ArrayProperty>(name); break;
        case aePropertyType::Float3Array:    return obj->newProperty<abcFloat3ArrayProperty>(name); break;
        case aePropertyType::Float4Array:    return obj->newProperty<abcFloat4ArrayProperty>(name); break;
        case aePropertyType::Float4x4Array:  return obj->newProperty<abcFloat4x4ArrayProperty>(name); break;
        default: break;
    }
    DebugLog("aeNewProperty(): unknown type");
    return nullptr;
}

abciAPI void aePropertyWriteArraySample(aeProperty *prop, const void *data, int num_data)
{
    if (!prop)
        return;

    if (!prop->isArray())
    {
        DebugLog("aePropertyWriteArraySample(): property is scalar!");
        return;
    }
    prop->writeSample(data, num_data);
}

abciAPI void aePropertyWriteScalarSample(aeProperty *prop, const void *data)
{
    if (!prop)
        return;

    if (prop->isArray())
    {
        DebugLog("aePropertyWriteScalarSample(): property is array!");
        return;
    }
    prop->writeSample(data, 1);
}

abciAPI int aeGenerateRemapIndices(int *remap, abcV3 *points, aeWeights4 *weights, int vertex_count)
{
    int ret = 0;

    MeshWelder welder;
    if (weights)
    {
        auto compare_op = [&](int vi, int ni) { return weights[vi] == weights[ni]; };
        auto weld_op = [&](int vi, int ni) { weights[ni] = weights[vi]; };
        ret = welder.weld(points, vertex_count, compare_op, weld_op);
    }
    else
    {
        auto compare_op = [&](int vi, int ni) { return true; };
        auto weld_op = [&](int vi, int ni) {};
        ret = welder.weld(points, vertex_count, compare_op, weld_op);
    }
    CopyTo(welder.getRemapTable(), remap);
    return ret;
}

abciAPI void aeApplyMatrixP(abcV3 *dst_points, int num, const abcM44 *matrix)
{
    auto m = *matrix;
    for (int i = 0; i < num; i++)
    {
        m.multVecMatrix(dst_points[i], dst_points[i]);
    }
}

abciAPI void aeApplyMatrixV(abcV3 *dst_vectors, int num, const abcM44 *matrix)
{
    auto m = *matrix;
    for (int i = 0; i < num; i++)
    {
        m.multDirMatrix(dst_vectors[i], dst_vectors[i]);
    }
}
