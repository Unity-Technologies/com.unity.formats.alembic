#include "pch.h"
#include "../Exporter/AlembicExporter.h"
#include "../Importer/AlembicImporter.h"

struct MeshTriangulatorConfig
{
    bool    triangulate;
    bool    expand_indices;
    bool    split;
    bool    swap_handedness;
    bool    swap_face_winding;
    abcV3   scale;

    MeshTriangulatorConfig()
        : triangulate(true)
        , expand_indices(true)
        , split(false)
        , swap_handedness(false)
        , swap_face_winding(false)
        , scale(1.0f, 1.0f, 1.0f)
    {}
};

tCLinkage tExport bool MeshTriangulator(
    const char *src_abc_path,
    const char *dst_abc_path,
    const MeshTriangulatorConfig *conf)
{
    aiConfig iconf;
    aeConfig econf;

    aiContext *ictx = aiCreateContext(0);
    aiSetConfig(ictx, &iconf);
    if (!aiLoad(ictx, dst_abc_path)) {
        return false;
    }

    aeContext *ectx = aeCreateContext(&econf);
    if (!aeOpenArchive(ectx, dst_abc_path)) {
        aiDestroyContext(ictx);
        return false;
    }

    // todo

    aeDestroyContext(ectx);
    aiDestroyContext(ictx);
    return false;
}

int main(int argc, char *argv[])
{
    if (argc < 3) {
        printf("usage: %s [src .abc path] [dst .abc path] \n", argv[0]);
        return 1;
    }
    const char *src_abc_path = argv[1];
    const char *dst_abc_path = argv[2];
    MeshTriangulatorConfig conf;
    for (int i = 3; i < argc; ++i) {
        // todo: parse option params
    }

    return MeshTriangulator(src_abc_path, dst_abc_path, &conf) ? 0 : 1;
}
