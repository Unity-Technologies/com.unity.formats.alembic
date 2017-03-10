#include "pch.h"
#include "abci.h"
#include "aeContext.h"
#include "aeObject.h"
#include "aeXForm.h"
#include "aePoints.h"
#include "aePolyMesh.h"
#include "aeCamera.h"

#ifdef _WIN32
#pragma comment(lib, "Alembic.lib")
#pragma comment(lib, "libhdf5.lib")
#pragma comment(lib, "libhdf5_hl.lib")
#pragma comment(lib, "Half.lib")
#pragma comment(lib, "Iex-2_2.lib")
#pragma comment(lib, "IexMath-2_2.lib")
#endif


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
    case aePropertyType_Bool:           return parent->newProperty<abcBoolProperty>(name); break;
    case aePropertyType_Int:            return parent->newProperty<abcIntProperty>(name); break;
    case aePropertyType_UInt:           return parent->newProperty<abcUIntProperty>(name); break;
    case aePropertyType_Float:          return parent->newProperty<abcFloatProperty>(name); break;
    case aePropertyType_Float2:         return parent->newProperty<abcFloat2Property>(name); break;
    case aePropertyType_Float3:         return parent->newProperty<abcFloat3Property>(name); break;
    case aePropertyType_Float4:         return parent->newProperty<abcFloat4Property>(name); break;
    case aePropertyType_Float4x4:       return parent->newProperty<abcFloat4x4Property>(name); break;

        // array properties
    case aePropertyType_BoolArray:      return parent->newProperty<abcBoolArrayProperty >(name); break;
    case aePropertyType_IntArray:       return parent->newProperty<abcIntArrayProperty>(name); break;
    case aePropertyType_UIntArray:      return parent->newProperty<abcUIntArrayProperty>(name); break;
    case aePropertyType_FloatArray:     return parent->newProperty<abcFloatArrayProperty>(name); break;
    case aePropertyType_Float2Array:    return parent->newProperty<abcFloat2ArrayProperty>(name); break;
    case aePropertyType_Float3Array:    return parent->newProperty<abcFloat3ArrayProperty>(name); break;
    case aePropertyType_Float4Array:    return parent->newProperty<abcFloat4ArrayProperty>(name); break;
    case aePropertyType_Float4x4Array:  return parent->newProperty<abcFloat4x4ArrayProperty>(name); break;
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
