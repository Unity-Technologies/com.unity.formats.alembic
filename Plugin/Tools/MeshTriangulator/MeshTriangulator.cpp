#include "pch.h"
#include "Common.h"

struct MeshTriangulatorConfig
{
    bool    triangulate;
    bool    expand_indices;
    bool    split;
    bool    swap_handedness;
    bool    swap_faces;
    float   scale;
    // todo: normal option

    MeshTriangulatorConfig()
        : triangulate(true)
        , expand_indices(false)
        , split(false)
        , swap_handedness(false)
        , swap_faces(false)
        , scale(1.0f)
    {}
};


tCLinkage tExport bool tMeshTriangulator(
    const char *src_abc_path,
    const char *dst_abc_path,
    const MeshTriangulatorConfig *conf)
{
    if (!src_abc_path || !dst_abc_path || !conf) {
        tLog("tMeshTriangulator(): parameter is null");
        return false;
    }

    aiConfig iconf;
    iconf.swapHandedness = conf->swap_handedness;
    iconf.swapFaceWinding = conf->swap_faces;

    aeConfig econf;
    econf.scale = conf->scale;

    aiContext *ictx = aiCreateContext(0);
    aiSetConfig(ictx, &iconf);
    if (!aiLoad(ictx, src_abc_path)) {
        return false;
    }

    aeContext *ectx = aeCreateContext();
    if (!aeOpenArchive(ectx, dst_abc_path)) {
        aiDestroyContext(ictx);
        return false;
    }

    {
        tContext tctx;
        tctx.setExportConfig(econf);
        tctx.setArchives(ictx, ectx);

        tPolyMeshBuffer buf;
        tctx.setPolyMeshrocessor([&](aiPolyMesh *iobj, aePolyMesh *eobj) {
            int n = aiSchemaGetNumSamples(iobj);
            for (int i = 0; i < n; ++i) {
                auto ss = aiIndexToSampleSelector(i);
                aiSchemaUpdateSample(iobj, &ss);

                auto *sample = aiSchemaGetSample(iobj, &ss);
                aiPolyMeshData idata;
                aiPolyMeshGetDataPointer(sample, &idata);

                // make buffers to store triangulated mesh data
                if (idata.positions) {
                    buf.allocatePositions(idata.triangulatedIndexCount, idata.triangulatedIndexCount);
                }
                if (idata.velocities) {
                    buf.allocateVelocity(true);
                }
                if (idata.normals) {
                    buf.allocateNormals(idata.triangulatedIndexCount);
                }
                if (idata.uvs) {
                    buf.allocateUVs(idata.triangulatedIndexCount);
                }
                if (idata.faces) {
                    buf.allocateFaces(idata.triangulatedIndexCount / 3);
                }

                auto triangulated = buf.asImportData();
                aiPolyMeshCopyData(sample, &triangulated, true, conf->expand_indices);

                auto edata = tImportDataToExportData(triangulated);
                aePolyMeshWriteSample(eobj, &edata);
            }
        });

        tctx.doExport();
    }

    aeDestroyContext(ectx);
    aiDestroyContext(ictx);
    return false;
}

tCLinkage tExport bool tMeshTriangulator_CommandLine(int argc, char *argv[])
{
    if (argc < 3) { return false; }

    const char *src_abc_path = argv[1];
    const char *dst_abc_path = argv[2];
    MeshTriangulatorConfig conf;

    // parse options
    for (int i = 3; i < argc; ++i) {
        if (strcmp(argv[i], "/triangulate") == 0) {
            conf.triangulate = true;
        }
        else if (strcmp(argv[i], "/expand_indices") == 0) {
            conf.expand_indices = true;
        }
        else if (strcmp(argv[i], "/split") == 0) {
            conf.split = true;
        }
        else if (strcmp(argv[i], "/swap_handedness") == 0) {
            conf.swap_handedness = true;
        }
        else if (strcmp(argv[i], "/swap_faces") == 0) {
            conf.swap_faces = true;
        }
        else if (strncmp(argv[i], "/scale", 6) == 0) {
            float v;
            if (sscanf(argv[i], "/scale:%f", &v) == 1) {
                conf.scale = v;
            }
        }
    }
    return tMeshTriangulator(src_abc_path, dst_abc_path, &conf) ? 0 : 1;
}

int main(int argc, char *argv[])
{
    if (argc < 3) {
        printf(
            "usage: MeshTriangulator [source .abc path] [destination .abc path] [options]\n"
            "options:\n"
            "    /expand_indices\n"
            "    /split\n"
            "    /swap_handedness\n"
            "    /swap_faces\n"
            "    /scale:1.0,1.0,1.0\n"
            );
        return 1;
    }
    return tMeshTriangulator_CommandLine(argc, argv) ? 0 : 1;
}
