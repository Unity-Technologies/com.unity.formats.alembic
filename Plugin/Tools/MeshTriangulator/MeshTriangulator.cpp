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
    aiConfig iconf;
    aeConfig econf;

    aiContext *ictx = aiCreateContext(0);
    aiSetConfig(ictx, &iconf);
    if (!aiLoad(ictx, src_abc_path)) {
        return false;
    }

    aeContext *ectx = aeCreateContext(&econf);
    if (!aeOpenArchive(ectx, dst_abc_path)) {
        aiDestroyContext(ictx);
        return false;
    }

    {
        tContext tctx;
        tctx.setArchives(ictx, ectx);
        // todo
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
