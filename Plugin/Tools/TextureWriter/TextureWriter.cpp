#include "pch.h"
#include "Common.h"
#include "GraphicsDevice.h"


tCLinkage tExport bool tPointsCopyPositionsToTexture(aiPointsData *data, void *tex, int width, int height, tTextureFormat fmt)
{
    if (data == nullptr) { return false; }

    if (fmt == tTextureFormat_ARGBFloat)
    {
        return tWriteTextureWithConversion(tex, width, height, fmt, data->positions, data->count,
            [](void *dst, const abcV3 *src, int i) {
            ((abcV4*)dst)[i] = abcV4(src[i].x, src[i].y, src[i].z, 0.0f);
        });
    }
    else {
        tLog("aiPointsCopyPositionsToTexture(): format must be ARGBFloat");
        return false;
    }
}

tCLinkage tExport bool tPointsCopyIDsToTexture(aiPointsData *data, void *tex, int width, int height, tTextureFormat fmt)
{
    if (data == nullptr) { return false; }

    if (fmt == tTextureFormat_RInt)
    {
        return tWriteTextureWithConversion(tex, width, height, fmt, data->ids, data->count,
            [](void *dst, const uint64_t *src, int i) {
            ((int32_t*)dst)[i] = (int32_t)(src[i]);
        });
    }
    else if (fmt == tTextureFormat_RFloat)
    {
        return tWriteTextureWithConversion(tex, width, height, fmt, data->ids, data->count,
            [](void *dst, const uint64_t *src, int i) {
            ((float*)dst)[i] = (float)(src[i]);
        });
    }
    else
    {
        tLog("aiPointsCopyIDsToTexture(): format must be RFloat or RInt");
        return false;
    }
}
