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
        , random_diffuse(0.2f, 0.2f, 0.2f)
        , repulse_iteration(2)
        , repulse_particle_size(0.4f)
        , repulse_timestep(1.0f / 60.0f)
    {}
};

struct tPointInfo
{
    uint32_t id;
    abcV3 random_diffuse;
};

struct tPointInfoHeader
{
    uint64_t num;

    tPointInfo& operator[](size_t i) { return ((tPointInfo*)(this + 1))[i]; }
    tPointInfo& push() { return (*this)[num++]; }

    template<class Body>
    void each(const Body& body) {
        for (uint64_t i = 0; i < num; ++i) { body((*this)[i]); }
    }
};


tCLinkage tExport void tPointsRanfomizerConvert(tContext *tctx_, const PointsRandomizerConfig *conf)
{
    tContext& tctx = *tctx_;

    tctx.setPointsProcessor([&](aiPoints *iobj, aePoints *eobj) {
        tPointsBuffer buf;
        std::vector<uint64_t> ids, ids_tmp1, ids_tmp2;
        std::vector<char> point_info;

        aiPointsSummary summary;
        aiPointsGetSummary(iobj, &summary);


        int num_samples = aiSchemaGetNumSamples(iobj);

        // build list of all ids
        tLog("building list of all ids...\n");
        for (int i = 0; i < num_samples; ++i) {
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
            ids.assign(ids_tmp2.begin(), ids_tmp2.end());
        }

        // 
        size_t info_size = size_t(sizeof(tPointInfoHeader) + sizeof(tPointInfo) * std::ceil(conf->count_rate));
        uint64_t id_range = summary.maxID - summary.minID + 1;
        point_info.resize(info_size * id_range);

        auto getPointInfoByID = [&](size_t id) -> tPointInfoHeader& {
            return (tPointInfoHeader&)point_info[info_size * (id - summary.minID)];
        };

        uint64_t id_size_scaled = uint64_t((double)ids.size() * conf->count_rate);
        for (size_t pi = 0; pi < id_size_scaled; ++pi) {
            size_t spi = size_t((double)pi / conf->count_rate);
            size_t id = ids[spi];
            tPointInfo &pf = getPointInfoByID(id).push();
            pf.id = (uint32_t)pi;
            pf.random_diffuse = conf->random_diffuse * tRandV3();
        }


        mpKernelParams mpparams;
        mpparams.damping = 0.1f;
        mpparams.max_particles = int(summary.peakCount * std::ceil(conf->count_rate));
        mpparams.particle_size = conf->repulse_particle_size;
        auto mp = mpCreateContext();

        // process all frames
        for (int fi = 0; fi < num_samples; ++fi) {
            double repulsion_begin_time = tGetTime();
            auto ss = aiIndexToSampleSelector(fi);

            // get points data from alembic
            aiSchemaUpdateSample(iobj, &ss);
            auto *sample = aiSchemaGetSample(iobj, &ss);
            aiPointsData idata;
            aiPointsGetDataPointer(sample, &idata);

            // create inc/decreased points buffer
            size_t num_scaled = 0;
            for (size_t pi = 0; pi < idata.count; ++pi) {
                tPointInfoHeader& pinfo = getPointInfoByID(idata.ids[pi]);
                num_scaled += pinfo.num;
            }
            buf.allocate(num_scaled, idata.velocities != nullptr);

            for (size_t pi = 0, spi = 0; pi < idata.count; ++pi) {
                getPointInfoByID(idata.ids[pi]).each([&](const tPointInfo &pinfo) {
                    buf.positions[spi] = idata.positions[pi] + pinfo.random_diffuse;
                    buf.ids[spi] = pinfo.id;
                    if (idata.velocities) {
                        buf.velocities[spi] = idata.velocities[pi];
                    }
                    ++spi;
                });
            }


            // repulsion
            if (conf->repulse_iteration > 0) {
                tLog("calculating repulsion...\n");

                mpparams.world_center = (mpV3&)idata.center;
                mpparams.world_extent = (mpV3&)idata.size;
                mpparams.world_div = mpV3i(
                    std::min<int>(int(mpparams.world_extent.x * 2.0f / conf->repulse_particle_size), 256),
                    std::min<int>(int(mpparams.world_extent.y * 2.0f / conf->repulse_particle_size), 256),
                    std::min<int>(int(mpparams.world_extent.z * 2.0f / conf->repulse_particle_size), 256) );

                // apply repulsion
                mpSetKernelParams(mp, &mpparams);
                mpClearParticles(mp);
                mpForceSetNumParticles(mp, (int)num_scaled);
                mpParticle *particles = mpGetParticles(mp);
                for (size_t pi = 0; pi < num_scaled; ++pi) {
                    particles[pi].position = (mpV3&)buf.positions[pi];
                    particles[pi].velocity = mpV3();
                    particles[pi].lifetime = std::numeric_limits<float>::max();
                    particles[pi].userdata = (int)pi;
                }
                for (int ri = 0; ri < conf->repulse_iteration; ++ri) {
                    mpUpdate(mp, conf->repulse_timestep);
                }
                ist::parallel_sort(particles, particles + num_scaled,
                    [](const mpParticle& a, const mpParticle& b) { return a.userdata < b.userdata; });

                for (int pi = 0; pi < num_scaled; ++pi) {
                    buf.positions[pi] = (abcV3&)particles[pi].position;
                }
                if (idata.velocities) {
                    for (int pi = 0; pi < num_scaled; ++pi) {
                        buf.velocities[pi] = (abcV3&)particles[pi].velocity;
                    }
                }
                tLog("  frame %d: %.2lf ms (%d iteration)\n", fi, tGetTime() - repulsion_begin_time, conf->repulse_iteration);
            }


            auto edata = buf.asExportData();
            aePointsWriteSample(eobj, &edata);
        }
        mpDestroyContext(mp);
    });

    tctx.doExport();
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
