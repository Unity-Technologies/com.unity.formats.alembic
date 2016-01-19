#include "pch.h"
#include "Common.h"

struct MeshTriangulatorConfig
{
    bool    triangulate;
    bool    expand_indices;
    bool    split;
    bool    swap_handedness;
    bool    swap_faces;
    abcV3   scale;

    MeshTriangulatorConfig()
        : triangulate(true)
        , expand_indices(false)
        , split(false)
        , swap_handedness(false)
        , swap_faces(false)
        , scale(1.0f, 1.0f, 1.0f)
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
        tctx.setArchives(ictx, ectx);

        std::vector<abcV3> positions;
        std::vector<abcV3> velocities;
        std::vector<abcV3> normals;
        std::vector<abcV2> uvs;
        std::vector<int> indices;
        std::vector<int> faces;

        tctx.setPolyMeshrocessor([&](aiPolyMesh *iobj, aePolyMesh *eobj) {
            aiPolyMeshData idata;
            aiPolyMeshData triangulated;
            aePolyMeshData edata;
            int n = aiSchemaGetNumSamples(iobj);
            for (int i = 0; i < n; ++i) {
                auto ss = aiIndexToSampleSelector(i);
                aiSchemaUpdateSample(iobj, &ss);

                auto *sample = aiSchemaGetSample(iobj, &ss);
                aiPolyMeshGetDataPointer(sample, &idata);

                // make buffers to store triangle mesh data
                if (idata.positions) {
                    positions.resize(idata.triangulatedIndexCount);
                    triangulated.positions = &positions[0];
                    triangulated.positionCount = (int)positions.size();
                }
                if (idata.velocities) {
                    velocities.resize(idata.triangulatedIndexCount);
                    triangulated.velocities = &velocities[0];
                }
                if (idata.normals) {
                    normals.resize(idata.triangulatedIndexCount);
                    triangulated.normals = &normals[0];
                    triangulated.normalCount = (int)normals.size();
                }
                if (idata.uvs) {
                    uvs.resize(idata.triangulatedIndexCount);
                    triangulated.uvs = &uvs[0];
                    triangulated.uvCount = (int)uvs.size();
                }
                if(idata.indices) {
                    indices.resize(idata.triangulatedIndexCount);
                    triangulated.indices = &indices[0];
                    triangulated.indexCount = (int)indices.size();
                }
                if (idata.faces) {
                    faces.resize(idata.triangulatedIndexCount / 3);
                    triangulated.faces = &faces[0];
                    triangulated.faceCount = (int)faces.size();
                }

                aiPolyMeshCopyData(sample, &triangulated, true, conf->expand_indices);

                edata.positions = triangulated.positions;
                edata.velocities = triangulated.velocities;
                edata.normals = triangulated.normals;
                edata.uvs = triangulated.uvs;
                edata.indices = triangulated.indices;
                edata.faces = triangulated.faces;

                edata.positionCount = triangulated.positionCount;
                edata.normalCount = triangulated.normalCount;
                edata.uvCount = triangulated.uvCount;
                edata.indexCount = triangulated.indexCount;
                edata.faceCount = triangulated.faceCount;

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
            abcV3 v;
            if (sscanf(argv[i], "/scale:%f,%f,%f", &v.x, &v.y, &v.z) == 3) {
                conf.scale = v;
            }
        }
    }
    return tMeshTriangulator(src_abc_path, dst_abc_path, &conf) ? 0 : 1;
}

int main(int argc, char *argv[])
{
    if (argc < 3) {
        printf("usage: MeshTriangulator [src .abc path] [dst .abc path] (options - /triangulate /expand_indices /split /swap_handedness /swap_faces /scale:1.0,1.0,1.0)\n");
        return 1;
    }
    return tMeshTriangulator_CommandLine(argc, argv) ? 0 : 1;
}
