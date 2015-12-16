#include "pch.h"
#include "AlembicExporter.h"
#include "aeContext.h"
#include "aeObject.h"
#include "aeXForm.h"
#include "aePoints.h"
#include "aePolyMesh.h"
#include "aeCamera.h"

aeCLinkage aeExport void aeCleanup()
{

}

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

aeCLinkage aeExport aeObject* aeCreateObject(aeObject *parent, const char *name)
{
    return parent->getContext()->createObject(parent, name);
}

aeCLinkage aeExport void aeSetTime(aeContext* ctx, float time)
{
    ctx->setTime(time);
}


aeCLinkage aeExport aeXForm* aeAddXForm(aeObject *obj)
{
    return &obj->addXForm();
}

aeCLinkage aeExport aePoints* aeAddPoints(aeObject *obj)
{
    return &obj->addPoints();
}

aeCLinkage aeExport aePolyMesh* aeAddPolyMesh(aeObject *obj)
{
    return &obj->addPolyMesh();
}

aeCLinkage aeExport aeCamera* aeAddCamera(aeObject *obj)
{
    return &obj->addCamera();
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
