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

aeCLinkage aeExport void aeSetTime(aeContext* ctx, float time)
{
    ctx->setTime(time);
}


aeCLinkage aeExport aeXForm* aeNewXForm(aeObject *parent, const char *name)
{
    return parent->newChild<aeXForm>(name);
}
aeCLinkage aeExport aePoints* aeNewPoints(aeObject *parent, const char *name)
{
    return parent->newChild<aePoints>(name);
}
aeCLinkage aeExport aePolyMesh* aeNewPolyMesh(aeObject *parent, const char *name)
{
    return parent->newChild<aePolyMesh>(name);
}
aeCLinkage aeExport aeCamera* aeNewCamera(aeObject *parent, const char *name)
{
    return parent->newChild<aeCamera>(name);
}

aeCLinkage aeExport void aeXFormWriteSample(aeXForm *obj, const aeXFormSampleData *data)
{
    obj->writeSample(*data);
}
aeCLinkage aeExport void aePointsWriteSample(aePoints *obj, const aePointsSampleData *data)
{
    obj->writeSample(*data);
}
aeCLinkage aeExport void aePolyMeshWriteSample(aePolyMesh *obj, const aePolyMeshSampleData *data)
{
    obj->writeSample(*data);
}
aeCLinkage aeExport void aeCameraWriteSample(aeCamera *obj, const aeCameraSampleData *data)
{
    obj->writeSample(*data);
}
