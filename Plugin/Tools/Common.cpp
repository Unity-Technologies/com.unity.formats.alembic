#include "pch.h"
#include "Common.h"

void tSimpleCopyXForm(aiXForm *iobj, aeXForm *eobj)
{
    aiXFormData idata;
    aeXFormData edata;
    int n = aiSchemaGetNumSamples(iobj);
    for (int i = 0; i < n; ++i) {
        auto ss = aiIndexToSampleSelector(i);
        aiSchemaUpdateSample(iobj, &ss);
        auto *sample = aiSchemaGetSample(iobj, &ss);
        aiXFormGetData(sample, &idata);

        edata.translation   = idata.translation;
        edata.rotation      = idata.rotation;
        edata.scale         = idata.scale;
        edata.inherits      = idata.inherits;

        aeXFormWriteSample(eobj, &edata);
    }
}

void tSimpleCopyCamera(aiCamera *iobj, aeCamera *eobj)
{
    aiCameraData idata;
    aeCameraData edata;
    int n = aiSchemaGetNumSamples(iobj);
    for (int i = 0; i < n; ++i) {
        auto ss = aiIndexToSampleSelector(i);
        aiSchemaUpdateSample(iobj, &ss);
        auto *sample = aiSchemaGetSample(iobj, &ss);
        aiCameraGetData(sample, &idata);

        edata.nearClippingPlane = idata.nearClippingPlane;
        edata.farClippingPlane  = idata.farClippingPlane;
        edata.fieldOfView       = idata.fieldOfView;
        edata.focusDistance     = idata.focusDistance;
        edata.focalLength       = idata.focalLength;
        aeCameraWriteSample(eobj, &edata);
    }
}

void tSimpleCopyPolyMesh(aiPolyMesh *iobj, aePolyMesh *eobj)
{
    aiPolyMeshData idata;
    aePolyMeshData edata;
    int n = aiSchemaGetNumSamples(iobj);
    for (int i = 0; i < n; ++i) {
        auto ss = aiIndexToSampleSelector(i);
        aiSchemaUpdateSample(iobj, &ss);
        auto *sample = aiSchemaGetSample(iobj, &ss);
        aiPolyMeshGetDataPointer(sample, &idata);

        edata.positions     = idata.positions;
        edata.velocities    = idata.velocities;
        edata.normals       = idata.normals;
        edata.uvs           = idata.uvs;
        edata.indices       = idata.indices;
        edata.normalIndices = idata.normalIndices;
        edata.uvIndices     = idata.uvIndices;
        edata.faces         = idata.faces;

        edata.positionCount     = idata.positionCount;
        edata.normalCount       = idata.normalCount;
        edata.uvCount           = idata.uvCount;
        edata.indexCount        = idata.indexCount;
        edata.normalIndexCount  = idata.normalIndexCount;
        edata.uvIndexCount      = idata.uvIndexCount;
        edata.faceCount         = idata.faceCount;

        aePolyMeshWriteSample(eobj, &edata);
    }
}

void tSimpleCopyPoints(aiPoints *iobj, aePoints *eobj)
{
    aiPointsData idata;
    aePointsData edata;
    int n = aiSchemaGetNumSamples(iobj);
    for (int i = 0; i < n; ++i) {
        auto ss = aiIndexToSampleSelector(i);
        aiSchemaUpdateSample(iobj, &ss);
        auto *sample = aiSchemaGetSample(iobj, &ss);
        aiPointsGetDataPointer(sample, &idata);

        edata.positions     = idata.positions;
        edata.velocities    = idata.velocities;
        edata.ids           = idata.ids;
        edata.count         = idata.count;

        aePointsWriteSample(eobj, &edata);
    }
}


tContext::tContext()
    : m_ictx(nullptr), m_ectx(nullptr)
    , m_xfproc(tSimpleCopyXForm)
    , m_camproc(tSimpleCopyCamera)
    , m_meshproc(tSimpleCopyPolyMesh)
    , m_pointsproc(tSimpleCopyPoints)
{
    
}

tContext::~tContext()
{
}

void tContext::setArchives(aiContext *ictx, aeContext *ectx)
{
    m_ictx = ictx;
    m_ectx = ectx;
}

aiObject* tContext::getIObject(aeObject *eobj)
{
    auto i = m_eimap.find(eobj);
    return i == m_eimap.end() ? nullptr : i->second;
}

aeObject* tContext::getEObject(aiObject *iobj)
{
    auto i = m_iemap.find(iobj);
    return i == m_iemap.end() ? nullptr : i->second;
}

void tContext::setXFormProcessor(const XFormProcessor& v)       { m_xfproc = v; }
void tContext::setCameraProcessor(const CameraProcessor& v)     { m_camproc = v; }
void tContext::setPointsProcessor(const PointsProcessor& v)     { m_pointsproc = v; }
void tContext::setPolyMeshrocessor(const PolyMeshProcessor& v)  { m_meshproc = v; }

void tContext::doExport()
{
    if (!m_ictx || !m_ectx) { return; }

    aiTimeSamplingData tsd;
    int tsi = aiGetNumTimeSamplings(m_ictx) > 1 ? 1 : 0;
    aiGetTimeSampling(m_ictx, tsi, &tsd);

    aeConfig econf;
    if (tsd.type == aiTimeSamplingType_Uniform) {
        econf.timeSamplingType = aeTimeSamplingType_Uniform;
        econf.startTime = tsd.startTime;
        econf.frameRate = 1.0f / tsd.interval;
    }
    else if (tsd.type == aiTimeSamplingType_Cyclic) {
        econf.timeSamplingType = aeTimeSamplingType_Cyclic;
        econf.startTime = tsd.startTime;
        econf.frameRate = 1.0f / tsd.interval;
    }
    else if (tsd.type == aiTimeSamplingType_Acyclic) {
        econf.timeSamplingType = aeTimeSamplingType_Acyclic;
        econf.startTime = tsd.startTime;
        econf.frameRate = 1.0f / tsd.interval;
    }
    aeSetConfig(m_ectx, &econf);
    for (int i = 0; i < tsd.numTimes; ++i) {
        aeAddTime(m_ectx, (float)tsd.times[i]);
    }

    doExportImpl(aiGetTopObject(m_ictx));
}

void tContext::doExportImpl(aiObject *iobj)
{
    aeObject *eobj = nullptr;
    if (iobj == aiGetTopObject(m_ictx)) {
        eobj = aeGetTopObject(m_ectx);
    }
    else {
        aeObject *eparent = m_estack.empty() ? nullptr : m_estack.back();
        const char *name = aiGetNameS(iobj);
        if (auto *ixf = aiGetXForm(iobj)) {
            auto *exf = aeNewXForm(eparent, name);
            if (m_xfproc) m_xfproc(ixf, exf);
            eobj = exf;
        }
        else if (auto *icam = aiGetCamera(iobj)) {
            auto *ecam = aeNewCamera(eparent, name);
            if (m_camproc) m_camproc(icam, ecam);
            eobj = ecam;
        }
        else if (auto *ipoints = aiGetPoints(iobj)) {
            auto *epoints = aeNewPoints(eparent, name);
            if (m_pointsproc) m_pointsproc(ipoints, epoints);
            eobj = epoints;
        }
        else if (auto *imesh = aiGetPolyMesh(iobj)) {
            auto *emesh = aeNewPolyMesh(eparent, name);
            if (m_meshproc) m_meshproc(imesh, emesh);
            eobj = emesh;
        }
    }

    m_iemap[iobj] = eobj;
    m_eimap[eobj] = iobj;
    m_istack.push_back(iobj);
    m_estack.push_back(eobj);

    int n = aiGetNumChildren(iobj);
    for (int i = 0; i < n; ++i) {
        auto *child = aiGetChild(iobj, i);
        doExportImpl(child);
    }

    m_estack.pop_back();
    m_istack.pop_back();
}
