#include "pch.h"
#include "Common.h"


struct PointsRandomizerSettings
{
    float   count_rate;
    abcV3   random_diffuse;
    int     repulse_iteration;
    float   repulse_timestep;

    PointsRandomizerSettings()
        : count_rate(1.0f)
        , random_diffuse(0.0f, 0.0f, 0.0f)
        , repulse_iteration(8)
        , repulse_timestep(1.0f / 60.0f)
    {}
};

tCLinkage tExport bool tPointsRanfomizer(
    const char *src_abc_path,
    const char *dst_abc_path,
    const PointsRandomizerSettings *settings)
{
    return false;
}

int main(int argc, char *argv[])
{

}
