#include "pch.h"
#include "AlembicExporter.h"
#include "aeContext.h"
#include "aeObject.h"
#include "aeXForm.h"
#include "aePoints.h"
#include "aePolyMesh.h"
#include "aeCamera.h"


aeCLinkage aeExport aeContext* aeCreateContext(const aeConfig *conf)
{
    return new aeContext(*conf);
}

aeCLinkage aeExport void aeDestroyContext(aeContext* ctx)
{
    delete ctx;
}

aeCLinkage aeExport bool aeOpenArchive(aeContext* ctx, const char *path)
{
    return ctx->openArchive(path);
}


aeCLinkage aeExport aeObject* aeGetTopObject(aeContext* ctx)
{
    return ctx->getTopObject();
}

aeCLinkage aeExport int aeAddTimeSampling(aeContext* ctx, float start_time)
{
    return ctx->addTimeSampling(start_time);
}

aeCLinkage aeExport void aeAddTime(aeContext* ctx, float time, int ts)
{
    ctx->setTime(time, ts);
}


aeCLinkage aeExport void aeDeleteObject(aeObject *obj)
{
    delete obj;
}
aeCLinkage aeExport aeXForm* aeNewXForm(aeObject *parent, const char *name, int tsi)
{
    return parent->newChild<aeXForm>(name, tsi);
}
aeCLinkage aeExport aePoints* aeNewPoints(aeObject *parent, const char *name, int tsi)
{
    return parent->newChild<aePoints>(name, tsi);
}
aeCLinkage aeExport aePolyMesh* aeNewPolyMesh(aeObject *parent, const char *name, int tsi)
{
    return parent->newChild<aePolyMesh>(name, tsi);
}
aeCLinkage aeExport aeCamera* aeNewCamera(aeObject *parent, const char *name, int tsi)
{
    return parent->newChild<aeCamera>(name, tsi);
}

aeCLinkage aeExport int aeGetNumChildren(aeObject *obj)
{
    return (int)obj->getNumChildren();
}
aeCLinkage aeExport aeObject* aeGetChild(aeObject *obj, int i)
{
    return obj->getChild(i);
}

aeCLinkage aeExport aeObject* aeGetParent(aeObject *obj)
{
    return obj->getParent();
}

aeCLinkage aeExport aeXForm* aeAsXForm(aeObject *obj)
{
    return dynamic_cast<aeXForm*>(obj);
}
aeCLinkage aeExport aePoints* aeAsPoints(aeObject *obj)
{
    return dynamic_cast<aePoints*>(obj);
}
aeCLinkage aeExport aePolyMesh* aeAsPolyMesh(aeObject *obj)
{
    return dynamic_cast<aePolyMesh*>(obj);
}
aeCLinkage aeExport aeCamera* aeAsCamera(aeObject *obj)
{
    return dynamic_cast<aeCamera*>(obj);
}


aeCLinkage aeExport int aeGetNumSamples(aeObject *obj)
{
    return (int)obj->getNumSamples();
}
aeCLinkage aeExport void aeSetFromPrevious(aeObject *obj)
{
    obj->setFromPrevious();
}

aeCLinkage aeExport void aeXFormWriteSample(aeXForm *obj, const aeXFormData *data)
{
    obj->writeSample(*data);
}
aeCLinkage aeExport void aePointsWriteSample(aePoints *obj, const aePointsData *data)
{
    obj->writeSample(*data);
}
aeCLinkage aeExport void aePolyMeshWriteSample(aePolyMesh *obj, const aePolyMeshData *data)
{
    obj->writeSample(*data);
}
aeCLinkage aeExport void aeCameraWriteSample(aeCamera *obj, const aeCameraData *data)
{
    obj->writeSample(*data);
}

aeCLinkage aeExport aeProperty* aeNewProperty(aeObject *parent, const char *name, aePropertyType type)
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
    aeDebugLog("aeNewProperty(): unknown type");
    return nullptr;
}

aeCLinkage aeExport void aePropertyWriteArraySample(aeProperty *prop, const void *data, int num_data)
{
    if (!prop->isArray()) {
        aeDebugLog("aePropertyWriteArraySample(): property is scalar!");
        return;
    }
    prop->writeSample(data, num_data);
}

aeCLinkage aeExport void aePropertyWriteScalarSample(aeProperty *prop, const void *data)
{
    if (prop->isArray()) {
        aeDebugLog("aePropertyWriteScalarSample(): property is array!");
        return;
    }
    prop->writeSample(data, 1);
}
