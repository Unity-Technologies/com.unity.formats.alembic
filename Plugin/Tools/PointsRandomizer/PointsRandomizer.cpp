#include "pch.h"
#include "Common.h"
#include "Concurrency.h"
#include "MassParticle.h"


struct PointsRandomizerConfig
{
    float   count_rate;
    abcV3   random_diffuse;
    int     repulse_iteration;
    float   repulse_particle_size;
    float   repulse_timestep;

    PointsRandomizerConfig()
        : count_rate(1.0f)
        , random_diffuse(0.0f, 0.0f, 0.0f)
        , repulse_iteration(8)
        , repulse_particle_size(0.1f)
        , repulse_timestep(1.0f / 60.0f)
    {}
};

struct tPointInfo
{
    uint64_t num;
    uint64_t ids[1];
};

tCLinkage tExport void tPointsRanfomizerConvert(tContext *tctx_, const PointsRandomizerConfig *conf)
{
    tContext& tctx = *tctx_;

    mpKernelParams mpparams;
    auto mp = mpCreateContext();

    tPointsBuffer buf;
    std::vector<uint64_t> ids, ids_tmp1, ids_tmp2;
    std::vector<char> info;
    std::vector<abcV3> random_diffuse; // tPointInfo
    tctx.setPointsProcessor([&](aiPoints *iobj, aePoints *eobj) {
        aiPointsSummary summary;
        aiPointsGetSummary(iobj, &summary);

        const float rcp_cr = 1.0f / conf->count_rate;
        uint64_t id_seed = 0;
        uint64_t id_begin = summary.minID;
        uint64_t id_range = summary.maxID - summary.minID + 1;
        uint64_t id_range_scaled = size_t((double)id_range * conf->count_rate);

        random_diffuse.resize(id_range_scaled);
        for (auto& v : random_diffuse) { v = tRandV3(); }

        mpparams.max_particles = summary.peakCount;
        mpparams.particle_size = conf->repulse_particle_size;

        int n = aiSchemaGetNumSamples(iobj);

        // build list of all ids
        tLog("building list of all ids...\n");
        for (int i = 0; i < n; ++i) {
            auto ss = aiIndexToSampleSelector(i);

            aiSchemaUpdateSample(iobj, &ss);
            auto *sample = aiSchemaGetSample(iobj, &ss);

            aiPointsData idata;
            aiPointsGetDataPointer(sample, &idata);

            ids_tmp1.assign(idata.ids, idata.ids + idata.count);
            ist::parallel_sort(ids_tmp1.begin(), ids_tmp1.end());

            ids_tmp2.resize(ids_tmp1.size() + ids.size());
            std::merge(ids_tmp1.begin(), ids_tmp1.end(), ids.begin(), ids.end(), ids_tmp2.begin());
            ids_tmp2.erase(std::unique(ids_tmp2.begin(), ids_tmp2.end()), ids_tmp2.end());
            ids.assign(ids_tmp1.begin(), ids_tmp1.end());
        }

        size_t info_size = size_t(8 * (1 + std::ceil(conf->count_rate)));
        info.resize(ids.size() * info_size);

        if (conf->repulse_iteration > 0) {
            tLog("calculating repulsion...\n");
            for (int i = 0; i < n; ++i) {
                double repulsion_begin_time = tGetTime();
                auto ss = aiIndexToSampleSelector(i);

                aiSchemaUpdateSample(iobj, &ss);
                auto *sample = aiSchemaGetSample(iobj, &ss);

                aiPointsData idata;
                aiPointsGetDataPointer(sample, &idata);

                mpparams.world_center = (mpV3&)idata.center;
                mpparams.world_extent = (mpV3&)idata.size;
                mpparams.world_div = mpV3i(
                    int(mpparams.world_extent.x * 2.0f / conf->repulse_particle_size),
                    int(mpparams.world_extent.y * 2.0f / conf->repulse_particle_size),
                    int(mpparams.world_extent.z * 2.0f / conf->repulse_particle_size) );

                // apply repulsion
                mpSetKernelParams(mp, &mpparams);
                mpClearParticles(mp);
                mpForceSetNumParticles(mp, idata.count);
                mpParticle *particles = mpGetParticles(mp);
                for (int i = 0; i < idata.count; ++i) {
                    particles[i].position = (mpV3&)idata.positions[i];
                    particles[i].velocity = mpV3();
                    particles[i].lifetime = std::numeric_limits<float>::max();
                    particles[i].userdata = i;
                }
                for (int i = 0; i < conf->repulse_iteration; ++i) {
                    mpUpdate(mp, conf->repulse_timestep);
                }
                std::sort(particles, particles + idata.count,
                    [](const mpParticle& a, const mpParticle& b) { return a.userdata < b.userdata; });

                buf.allocate(idata.count, idata.velocities != nullptr);
                for (int i = 0; i < idata.count; ++i) {
                    buf.positions[i] = (abcV3&)particles[i].position;
                    buf.ids[i] = idata.ids[i];
                }
                if (idata.velocities) {
                    for (int i = 0; i < idata.count; ++i) {
                        buf.velocities[i] = (abcV3&)particles[i].velocity;
                    }
                }

                auto edata = buf.asExportData();
                aePointsWriteSample(eobj, &edata);
                tLog("  frame %d: %.2lf ms (%d iteration)\n", i, tGetTime() - repulsion_begin_time, conf->repulse_iteration);
            }
        }
    });

    tctx.doExport();

    mpDestroyContext(mp);
}

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
        tPointsRanfomizerConvert(&tctx, conf);
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
