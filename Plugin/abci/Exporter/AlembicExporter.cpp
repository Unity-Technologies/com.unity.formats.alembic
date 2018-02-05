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
    return ctx->setConfig(*conf);
}

abciAPI bool aeOpenArchive(aeContext* ctx, const char *path)
{
    return ctx->openArchive(path);
}


abciAPI aeObject* aeGetTopObject(aeContext* ctx)
{
    return ctx->getTopObject();
}

abciAPI int aeAddTimeSampling(aeContext* ctx, float start_time)
{
    return ctx->addTimeSampling(start_time);
}

abciAPI void aeAddTime(aeContext* ctx, float time, int tsi)
{
    ctx->addTime(time, tsi);
}

abciAPI void aeMarkFrameBegin(aeContext* ctx)
{
    ctx->markFrameBegin();
}
abciAPI void aeMarkFrameEnd(aeContext* ctx)
{
    ctx->markFrameEnd();
}

abciAPI void aeDeleteObject(aeObject *obj)
{
    delete obj;
}
abciAPI aeXform* aeNewXform(aeObject *parent, const char *name, int tsi)
{
    return parent->newChild<aeXform>(name, tsi);
}
abciAPI aePoints* aeNewPoints(aeObject *parent, const char *name, int tsi)
{
    return parent->newChild<aePoints>(name, tsi);
}
abciAPI aePolyMesh* aeNewPolyMesh(aeObject *parent, const char *name, int tsi)
{
    return parent->newChild<aePolyMesh>(name, tsi);
}
abciAPI aeCamera* aeNewCamera(aeObject *parent, const char *name, int tsi)
{
    return parent->newChild<aeCamera>(name, tsi);
}

abciAPI int aeGetNumChildren(aeObject *obj)
{
    return (int)obj->getNumChildren();
}
abciAPI aeObject* aeGetChild(aeObject *obj, int i)
{
    return obj->getChild(i);
}

abciAPI aeObject* aeGetParent(aeObject *obj)
{
    return obj->getParent();
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


abciAPI int aeGetNumSamples(aeObject *obj)
{
    return (int)obj->getNumSamples();
}
abciAPI void aeSetFromPrevious(aeObject *obj)
{
    obj->setFromPrevious();
}

abciAPI void aeXformWriteSample(aeXform *obj, const aeXformData *data)
{
    obj->writeSample(*data);
}
abciAPI void aePointsWriteSample(aePoints *obj, const aePointsData *data)
{
    obj->writeSample(*data);
}
abciAPI void aePolyMeshWriteSample(aePolyMesh *obj, const aePolyMeshData *data)
{
    obj->writeSample(*data);
}
abciAPI int aePolyMeshAddFaceSet(aePolyMesh *obj, const char *name)
{
    return obj->addFaceSet(name);
}
abciAPI void aePolyMeshWriteFaceSetSample(aePolyMesh *obj, int fsi, const aeFaceSetData *data)
{
    obj->writeFaceSetSample(fsi, *data);
}
abciAPI void aeCameraWriteSample(aeCamera *obj, const aeCameraData *data)
{
    obj->writeSample(*data);
}

abciAPI aeProperty* aeNewProperty(aeObject *parent, const char *name, aePropertyType type)
{
    switch (type) {
        // scalar properties
    case aePropertyType::Bool:           return parent->newProperty<abcBoolProperty>(name); break;
    case aePropertyType::Int:            return parent->newProperty<abcIntProperty>(name); break;
    case aePropertyType::UInt:           return parent->newProperty<abcUIntProperty>(name); break;
    case aePropertyType::Float:          return parent->newProperty<abcFloatProperty>(name); break;
    case aePropertyType::Float2:         return parent->newProperty<abcFloat2Property>(name); break;
    case aePropertyType::Float3:         return parent->newProperty<abcFloat3Property>(name); break;
    case aePropertyType::Float4:         return parent->newProperty<abcFloat4Property>(name); break;
    case aePropertyType::Float4x4:       return parent->newProperty<abcFloat4x4Property>(name); break;

        // array properties
    case aePropertyType::BoolArray:      return parent->newProperty<abcBoolArrayProperty >(name); break;
    case aePropertyType::IntArray:       return parent->newProperty<abcIntArrayProperty>(name); break;
    case aePropertyType::UIntArray:      return parent->newProperty<abcUIntArrayProperty>(name); break;
    case aePropertyType::FloatArray:     return parent->newProperty<abcFloatArrayProperty>(name); break;
    case aePropertyType::Float2Array:    return parent->newProperty<abcFloat2ArrayProperty>(name); break;
    case aePropertyType::Float3Array:    return parent->newProperty<abcFloat3ArrayProperty>(name); break;
    case aePropertyType::Float4Array:    return parent->newProperty<abcFloat4ArrayProperty>(name); break;
    case aePropertyType::Float4x4Array:  return parent->newProperty<abcFloat4x4ArrayProperty>(name); break;
    }
    abciDebugLog("aeNewProperty(): unknown type");
    return nullptr;
}

abciAPI void aePropertyWriteArraySample(aeProperty *prop, const void *data, int num_data)
{
    if (!prop->isArray()) {
        abciDebugLog("aePropertyWriteArraySample(): property is scalar!");
        return;
    }
    prop->writeSample(data, num_data);
}

abciAPI void aePropertyWriteScalarSample(aeProperty *prop, const void *data)
{
    if (prop->isArray()) {
        abciDebugLog("aePropertyWriteScalarSample(): property is array!");
        return;
    }
    prop->writeSample(data, 1);
}


abciAPI int aeGenerateRemapIndices(int *remap, abcV3 *points, aeWeights4 *weights, int vertex_count)
{
    int ret = 0;

    MeshWelder welder;
    if (weights) {
        auto compare_op = [&](int vi, int ni) { return weights[vi] == weights[ni]; };
        auto weld_op = [&](int vi, int ni) { weights[ni] = weights[vi]; };
        ret = welder.weld(points, vertex_count, compare_op, weld_op);
    }
    else {
        auto compare_op = [&](int vi, int ni) { return true; };
        auto weld_op = [&](int vi, int ni) {};
        ret = welder.weld(points, vertex_count, compare_op, weld_op);
    }
    welder.getRemapTable().copy_to(remap);
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
