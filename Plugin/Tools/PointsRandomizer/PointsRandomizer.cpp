#include "pch.h"
#include "../Exporter/AlembicExporter.h"
#include "../Importer/AlembicImporter.h"


struct PointsRandomizerSettings
{
    float   count_rate;
    abcV3   random_diffuse;
    int     repulse_iteration;
    float   repulse_max_distance;

    PointsRandomizerSettings()
        : count_rate(1.0f)
        , random_diffuse(0.0f, 0.0f, 0.0f)
        , repulse_iteration(8)
        , repulse_max_distance(0.2f)
    {}
};

tCLinkage tExport bool PointsRanfomize(
    const char *src_abc_path,
    const char *dst_abc_path,
    const PointsRandomizerSettings *settings)
{
    return false;
}

int main(int argc, char *argv[])
{

}
