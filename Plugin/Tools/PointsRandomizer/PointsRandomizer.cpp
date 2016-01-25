#include "pch.h"
#include "Foundation.h"
#include "AlembicProcessor.h"
#include "Concurrency.h"
#include "MassParticle.h"


struct PointsRandomizerConfig
{
    float       count_rate;
    uint32_t    random_seed;
    float3      random_diffuse;
    int         repulse_iteration;
    float       repulse_particle_size;
    float       repulse_timestep;
    tLogCallback logCB;

    PointsRandomizerConfig()
        : count_rate(1.0f)
        , random_seed(0)
        , random_diffuse(0.2f, 0.2f, 0.2f)
        , repulse_iteration(2)
        , repulse_particle_size(0.4f)
        , repulse_timestep(1.0f / 60.0f)
        , logCB(nullptr)
    {}
};

struct tPointInfo
{
    uint32_t id;
    float3 random_diffuse;
};

struct tPointInfoHeader
{
    uint32_t num;

    tPointInfo& operator[](size_t i) { return ((tPointInfo*)(this + 1))[i]; }
    tPointInfo& push() { return (*this)[(size_t)(num++)]; }

    template<class Body>
    void each(const Body& body) {
        for (uint32_t i = 0; i < num; ++i) { body((*this)[i]); }
    }
};


tCLinkage tExport void tPointsRandomizerConvert(tContext *tctx_, const PointsRandomizerConfig *conf)
{
    tContext& tctx = *tctx_;
    tLogSetCallback(conf->logCB);

    tctx.setPointsProcessor([&](aiPoints *iobj, aePoints *eobj) {
        double time_proc_begin = tGetTime();
        const char* target_name = aiGetNameS(aiSchemaGetObject(iobj));
        tLog("processing \"%s\"\n", target_name);

        tPointsBuffer buf;
        std::vector<uint64_t> ids, ids_tmp1, ids_tmp2;
        std::vector<char> point_info;

        aiPointsSummary summary;
        aiPointsGetSummary(iobj, &summary);


        int num_samples = aiSchemaGetNumSamples(iobj);

        // build list of all ids
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
        tLog("  listed all IDs. %d elements (%.2lfms)\n", (int)ids.size(), tGetTime() - time_proc_begin);

        // 
        size_t info_size = size_t(sizeof(tPointInfoHeader) + sizeof(tPointInfo) * std::ceil(conf->count_rate));
        uint64_t id_range = summary.maxID - summary.minID + 1;
        point_info.resize(info_size * (size_t)id_range);

        auto getPointInfoByID = [&](uint64_t id) -> tPointInfoHeader& {
            return (tPointInfoHeader&)point_info[info_size * size_t(id - summary.minID)];
        };

        tRandSetSeed(conf->random_seed);
        uint64_t id_size_scaled = uint64_t((double)ids.size() * conf->count_rate);
        for (size_t pi = 0; pi < id_size_scaled; ++pi) {
            size_t spi = size_t((double)pi / conf->count_rate);
            uint64_t id = ids[spi];
            tPointInfo &pf = getPointInfoByID(id).push();
            pf.id = (uint32_t)pi;
            pf.random_diffuse = conf->random_diffuse * tRand3();
        }


        mpKernelParams mpparams;
        mpparams.damping = 0.1f;
        mpparams.max_particles = int(summary.peakCount * std::ceil(conf->count_rate));
        mpparams.particle_size = conf->repulse_particle_size;
        auto mp = mpCreateContext();

        // process all frames
        for (int fi = 0; fi < num_samples; ++fi) {
            double time_frame_begin = tGetTime();
            auto ss = aiIndexToSampleSelector(fi);

            // get points data from alembic
            aiSchemaUpdateSample(iobj, &ss);
            auto *sample = aiSchemaGetSample(iobj, &ss);
            aiPointsData idata;
            aiPointsGetDataPointer(sample, &idata);

            // create inc/decreased points buffer
            size_t num_scaled = 0;
            for (int pi = 0; pi < idata.count; ++pi) {
                tPointInfoHeader& pinfo = getPointInfoByID(idata.ids[pi]);
                num_scaled += pinfo.num;
            }
            buf.allocate(num_scaled, idata.velocities != nullptr);

            for (int pi = 0, spi = 0; pi < idata.count; ++pi) {
                getPointInfoByID(idata.ids[pi]).each([&](const tPointInfo &pinfo) {
                    buf.positions[spi] = (float3&)idata.positions[pi] + pinfo.random_diffuse;
                    buf.ids[spi] = pinfo.id;
                    if (idata.velocities) {
                        buf.velocities[spi] = (float3&)idata.velocities[pi];
                    }
                    ++spi;
                });
            }


            // repulsion
            if (conf->repulse_iteration > 0) {
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

                for (size_t pi = 0; pi < num_scaled; ++pi) {
                    buf.positions[pi] = (float3&)particles[pi].position;
                }
                if (idata.velocities) {
                    for (size_t pi = 0; pi < num_scaled; ++pi) {
                        buf.velocities[pi] = (float3&)particles[pi].velocity;
                    }
                }
            }

            auto edata = buf.asExportData();
            aePointsWriteSample(eobj, &edata);

            tLog("  frame %d: %d -> %d points (%.2lfms)\n",
                fi, idata.count, (int)num_scaled, tGetTime() - time_frame_begin);
        }
        mpDestroyContext(mp);

        tLog("finished \"%s\" (%.2lfms)\n", target_name, tGetTime() - time_proc_begin);
    });

    tctx.doExport();
}

tCLinkage tExport bool tPointsRandomizer(
    const char *src_abc_path,
    const char *dst_abc_path,
    const PointsRandomizerConfig *conf)
{
    if (!src_abc_path || !dst_abc_path || !conf) {
        tLog("tPointsRandomizer(): parameter is null\n");
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
        tPointsRandomizerConvert(&tctx, conf);
    }

    aeDestroyContext(ectx);
    aiDestroyContext(ictx);
    return false;
}

tCLinkage tExport bool tPointsRandomizerCommandLine(int argc, char *argv[])
{
    if (argc < 3) { return false; }

    const char *src_abc_path = argv[1];
    const char *dst_abc_path = argv[2];
    PointsRandomizerConfig conf;

    int vi;
    float vf;
    float3 v3f;
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
    return tPointsRandomizer(src_abc_path, dst_abc_path, &conf) ? 0 : 1;
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
    return tPointsRandomizerCommandLine(argc, argv) ? 0 : 1;
}
