#include "pch.h"
#include "aeInternal.h"
#include "aeContext.h"
#include "aeObject.h"
#include "aeXForm.h"
#include "aePoints.h"
#include "aePolyMesh.h"
#include "aeCamera.h"


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


abciAPI void aeDeleteObject(aeObject *obj)
{
    delete obj;
}
abciAPI aeXForm* aeNewXForm(aeObject *parent, const char *name, int tsi)
{
    return parent->newChild<aeXForm>(name, tsi);
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

abciAPI aeXForm* aeAsXForm(aeObject *obj)
{
    return dynamic_cast<aeXForm*>(obj);
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

abciAPI void aeXFormWriteSample(aeXForm *obj, const aeXFormData *data)
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


inline uint32_t hash(const abcV3& value)
{
    auto* h = (const uint32_t*)(&value);
    uint32_t f = (h[0] + h[1] * 11 - (h[2] * 17)) & 0x7fffffff;
    return (f >> 22) ^ (f >> 12) ^ (f);
}
inline int next_power_of_two(uint32_t v)
{
    v--;
    v |= v >> 1;
    v |= v >> 2;
    v |= v >> 4;
    v |= v >> 8;
    v |= v >> 16;
    v++;
    return v + (v == 0);
}
abciAPI int aeGenerateRemapIndices(int *remap, abcV3 *points, aeWeights4 *weights, int vertex_count)
{
    const int NIL = -1;
    int output_count = 0;
    int hash_size = next_power_of_two(vertex_count);
    RawVector<int> hash_table(hash_size + vertex_count);
    int* next = &hash_table[hash_size];

    memset(hash_table.data(), NIL, (hash_size) * sizeof(int));

    for (int i = 0; i < vertex_count; i++)
    {
        auto& v = points[i];
        uint32_t hash_value = hash(v) & (hash_size - 1);
        int offset = hash_table[hash_value];
        while (offset != NIL)
        {
            if (points[offset] == v)
            {
                if (!weights || weights[i] == weights[offset])
                    break;
            }
            offset = next[offset];
        }

        if (offset == NIL)
        {
            remap[i] = output_count;
            points[output_count] = v;
            if (weights)
                weights[output_count] = weights[i];

            next[output_count] = hash_table[hash_value];
            hash_table[hash_value] = output_count++;
        }
        else
        {
            remap[i] = offset;
        }
    }
    return output_count;
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
