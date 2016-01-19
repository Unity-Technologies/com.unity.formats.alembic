#include "pch.h"
#include "Common.h"


struct PointsRandomizerConfig
{
    float   count_rate;
    abcV3   random_diffuse;
    int     repulse_iteration;
    float   repulse_timestep;

    PointsRandomizerConfig()
        : count_rate(1.0f)
        , random_diffuse(0.0f, 0.0f, 0.0f)
        , repulse_iteration(8)
        , repulse_timestep(1.0f / 60.0f)
    {}
};

tCLinkage tExport bool tPointsRanfomizer(
    const char *src_abc_path,
    const char *dst_abc_path,
    const PointsRandomizerConfig *conf)
{
    if (!src_abc_path || !dst_abc_path || !conf) {
        tLog("tMeshTriangulator(): parameter is null");
        return false;
    }

    aiConfig iconf;
    aeConfig econf;

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

        tPointsBuffer buf;
        tctx.setPointsProcessor([&](aiPoints *iobj, aePoints *eobj) {
            int n = aiSchemaGetNumSamples(iobj);
            for (int i = 0; i < n; ++i) {
                auto ss = aiIndexToSampleSelector(i);

                aiSchemaUpdateSample(iobj, &ss);
                auto *sample = aiSchemaGetSample(iobj, &ss);

                aiPointsData idata;
                aiPointsGetDataPointer(sample, &idata);

                // todo

                auto edata = tImportDataToExportData(idata);
                aePointsWriteSample(eobj, &edata);
            }
        });

        tctx.doExport();
    }

    aeDestroyContext(ectx);
    aiDestroyContext(ictx);
    return false;
}

tCLinkage tExport bool tPointsRanfomizer_CommandLine(int argc, char *argv[])
{
    if (argc < 3) { return false; }

    const char *src_abc_path = argv[1];
    const char *dst_abc_path = argv[2];
    PointsRandomizerConfig conf;

    int vi;
    float vf;
    abcV3 v3f;
    // parse options
    for (int i = 3; i < argc; ++i) {
        if (sscanf(argv[i], "/count_rate:%f", &vf) == 1) {
            conf.count_rate = vf;
        }
        else if (sscanf(argv[i], "/random_diffuse:%f,%f,%f", &v3f.x, &v3f.y, &v3f.z) == 3) {
            conf.random_diffuse = v3f;
        }
        else if (sscanf(argv[i], "/repulse_iteration:%d", &vi) == 1) {
            conf.repulse_iteration = vi;
        }
        else if (sscanf(argv[i], "/repulse_timestep:%f", &vf) == 1) {
            conf.repulse_timestep = vf;
        }
    }
    return tPointsRanfomizer(src_abc_path, dst_abc_path, &conf) ? 0 : 1;
}

int main(int argc, char *argv[])
{
    if (argc < 3) {
        printf(
            "usage: PointsRandomizer [src .abc path] [dst .abc path] [options]\n"
            "options:\n"
            "    /count_rate:1.0\n"
            "    /random_diffuse:0.0,0.0,0.0\n"
            "    /repulse_iteration:5\n"
            "    /repulse_timestep:0.016666\n"
            );
        return 1;
    }
    return tPointsRanfomizer_CommandLine(argc, argv) ? 0 : 1;
}
