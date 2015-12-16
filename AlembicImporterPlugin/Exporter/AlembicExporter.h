#ifndef AlembicExporter_h
#define AlembicExporter_h


#include "pch.h"

#define aeCLinkage  aiCLinkage
#define aeExport    aiExport
#define aeDebugLog(...) 

struct aeConfig;

class aeContext;
class aeObject;
class aeSchemaBase;
class aeXForm;
class aePoints;
class aePolyMesh;
class aeCamera;
struct aeXFormSampleData;
struct aePointsSampleData;
struct aePolyMeshSampleData;
struct aeCameraSampleData;

aeCLinkage aeExport void            aeCleanup();
aeCLinkage aeExport aeContext*      aeCreateContext(const aeConfig *conf);
aeCLinkage aeExport void            aeDestroyContext(aeContext* ctx);
aeCLinkage aeExport bool            aeOpenArchive(aeContext* ctx, const char *path);

aeCLinkage aeExport aeObject*       aeGetTopObject(aeContext* ctx);
aeCLinkage aeExport aeObject*       aeCreateObject(aeObject *parent, const char *name);
aeCLinkage aeExport void            aeSetTime(aeContext* ctx, float time);

aeCLinkage aeExport aeXForm*        aeAddXForm(aeObject *obj);
aeCLinkage aeExport aePoints*       aeAddPoints(aeObject *obj);
aeCLinkage aeExport aePolyMesh*     aeAddPolyMesh(aeObject *obj);
aeCLinkage aeExport aeCamera*       aeAddCamera(aeObject *obj);
aeCLinkage aeExport void            aeXFormWriteSample(aeXForm *obj, const aeXFormSampleData *data);
aeCLinkage aeExport void            aePointsWriteSample(aePoints *obj, const aePointsSampleData *data);
aeCLinkage aeExport void            aePolyMeshWriteSample(aePolyMesh *obj, const aePolyMeshSampleData *data);
aeCLinkage aeExport void            aeCameraWriteSample(aeCamera *obj, const aeCameraSampleData *data);


#endif // AlembicExporter_h
