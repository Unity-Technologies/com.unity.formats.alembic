#include "pch.h"
#include "Test.h"
#include "../abci/abci.h"


TestCase(ExportAlembic_UVAndColorAnimation)
{
    aeConfig config;
    config.frame_rate = 1.0f;
    config.scale_factor = 100.0f;

    auto ctx = aeCreateContext();
    aeSetConfig(ctx, &config);
    aeOpenArchive(ctx, "UVAndColorAnimation.abc");

    auto top = aeGetTopObject(ctx);
    auto xf = aeNewXform(top, "UVAndColorAnimation");
    auto mesh = aeNewPolyMesh(xf, "UVAndColorAnimation_Mesh");
    {
        aePolyMeshData data;

        int counts[] = {4};
        int indices[] = {0, 1, 2, 3};
        abcV3 points[] = {
            { -0.5f, 0.0f, -0.5f },
            { -0.5f, 0.0f,  0.5f },
            {  0.5f, 0.0f,  0.5f },
            {  0.5f, 0.0f, -0.5f },
        };
        abcV2 uv0[] = {
            { 0.0f, 0.0f },
            { 0.0f, 1.0f },
            { 1.0f, 1.0f },
            { 1.0f, 0.0f },
        };
        abcV2 uv1[] = {
            { 1.0f, 1.0f },
            { 1.0f, 0.0f },
            { 1.0f, 0.0f },
            { 1.0f, 1.0f },
        };
        abcV4 colors[] = {
            { 1.0f, 0.0f, 0.0f, 1.0f },
            { 0.0f, 1.0f, 0.0f, 1.0f },
            { 0.0f, 0.0f, 1.0f, 1.0f },
            { 0.0f, 0.0f, 0.0f, 1.0f },
        };

        data.faces = counts;
        data.face_count = 1;
        data.indices = indices;
        data.index_count = 4;
        data.points = points;
        data.point_count = 4;

        data.uv0 = uv0;
        data.uv0_count = 4;
        data.uv1 = uv1;
        data.uv1_count = 4;
        data.colors = colors;
        data.colors_count = 4;

        aeMarkFrameBegin(ctx);
        aePolyMeshWriteSample(mesh, &data);
        aeMarkFrameEnd(ctx);

        for (int i = 0; i < 4; ++i) {
            uv0[i].x += 1.0f;
            uv1[i].y -= 1.0f;
            colors[i].x += 1.0f;
            colors[i].y += 1.0f;
            colors[i].z += 1.0f;
        }
        aeMarkFrameBegin(ctx);
        aePolyMeshWriteSample(mesh, &data);
        aeMarkFrameEnd(ctx);
    }
    aeDestroyContext(ctx);
}
