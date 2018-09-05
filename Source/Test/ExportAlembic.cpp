#include "pch.h"
#include "../abci/abci.h"
#include "MeshGenerator.h"
#include "Test.h"


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

TestCase(ExportAlembic_VisibilityAnimation)
{
    std::vector<int> counts, indices;
    std::vector<float3> points;
    std::vector<float2> uv;
    GenerateCylinderMesh(counts, indices, points, uv, 0.5f, 0.5f, 32, 16);

    aePolyMeshData mesh_data;
    mesh_data.faces = counts.data();
    mesh_data.face_count = (int)counts.size();
    mesh_data.indices = indices.data();
    mesh_data.index_count = (int)indices.size();
    mesh_data.points = (abcV3*)points.data();
    mesh_data.point_count = (int)points.size();
    mesh_data.uv0 = (abcV2*)uv.data();
    mesh_data.uv0_count = (int)uv.size();


    aeConfig config;
    config.frame_rate = 2.0f;
    config.scale_factor = 100.0f;

    auto ctx = aeCreateContext();
    aeSetConfig(ctx, &config);
    aeOpenArchive(ctx, "VisibilityAnimation.abc");

    auto top = aeGetTopObject(ctx);
    auto xf = aeNewXform(top, "VisibilityAnimation");
    auto mesh = aeNewPolyMesh(xf, "VisibilityAnimation_Mesh");
    {
        for (int i = 0; i < 10; ++i) {
            mesh_data.visibility = i % 2 == 0;

            aeMarkFrameBegin(ctx);
            aePolyMeshWriteSample(mesh, &mesh_data);
            aeMarkFrameEnd(ctx);
        }
    }
    aeDestroyContext(ctx);
}

TestCase(ExportAlembic_MultipleTimeSampling)
{
    aeConfig config;
    config.frame_rate = 2.0f;
    config.scale_factor = 100.0f;

    aeXformData xf_data[3];

    aePolyMeshData mesh_data;
    int counts[] = { 4 };
    int indices[] = { 0, 1, 2, 3 };
    abcV3 points[] = {
        {-0.5f, 0.0f, -0.5f },
        {-0.5f, 0.0f,  0.5f },
        { 0.5f, 0.0f,  0.5f },
        { 0.5f, 0.0f, -0.5f },
    };

    mesh_data.faces = counts;
    mesh_data.face_count = 1;
    mesh_data.indices = indices;
    mesh_data.index_count = 4;
    mesh_data.points = points;
    mesh_data.point_count = 4;


    auto ctx = aeCreateContext();
    aeSetConfig(ctx, &config);
    aeOpenArchive(ctx, "MultipleTimeSampling.abc");

    auto top = aeGetTopObject(ctx);
    int tsi[3] = {
        aeAddTimeSampling(ctx, 0.0f),
        aeAddTimeSampling(ctx, 2.5f),
        aeAddTimeSampling(ctx, 5.0f),
    };
    aeXform *xform[3] = {
        aeNewXform(top, "Obj1", tsi[0]),
        aeNewXform(top, "Obj2", tsi[1]),
        aeNewXform(top, "Obj3", tsi[2]),
    };
    aePolyMesh *mesh[3] = {
        aeNewPolyMesh(xform[0], "Mesh", tsi[0]),
        aeNewPolyMesh(xform[1], "Mesh", tsi[1]),
        aeNewPolyMesh(xform[2], "Mesh", tsi[2]),
    };

    for (int fi = 0; fi < 10; ++fi) {
        xf_data[0].translation.x = 0.1f * fi - 1.0f;
        xf_data[1].translation.y = 0.1f * fi - 1.0f;
        xf_data[2].translation.z = 0.1f * fi - 1.0f;

        aeMarkFrameBegin(ctx);
        for (int oi = 0; oi < 3; ++oi) {
            aeXformWriteSample(xform[oi], &xf_data[oi]);
            aePolyMeshWriteSample(mesh[oi], &mesh_data);
        }
        aeMarkFrameEnd(ctx);
    }

    aeDestroyContext(ctx);
}

TestCase(ExportAlembic_LinesAndPoints)
{
    std::vector<int> counts_poly, indices_poly;
    std::vector<int> counts_lines, indices_lines;
    std::vector<int> counts_points, indices_points;
    std::vector<int> counts_mixed, indices_mixed;
    std::vector<float3> points;
    std::vector<float2> uv;
    GenerateCylinderMesh(counts_poly, indices_poly, points, uv, 0.2f, 1.0f, 32, 16);

    {
        counts_points.resize(points.size());
        indices_points.resize(points.size());
        std::fill(counts_points.begin(), counts_points.end(), 1);
        std::iota(indices_points.begin(), indices_points.end(), 0);
    }

    {
        int n = 0;
        for (int c : counts_poly) {
            for (int i = 0; i < c; ++i) {
                indices_lines.push_back(indices_poly[n + i]);
                indices_lines.push_back(indices_poly[n + (i + 1) % c]);
                counts_lines.push_back(2);
            }
            n += c;
        }
    }

    {
        counts_mixed.insert(counts_mixed.end(), counts_lines.begin(), counts_lines.begin() + counts_lines.size() / 4);
        indices_mixed.insert(indices_mixed.end(), indices_lines.begin(), indices_lines.begin() + indices_lines.size() / 4);
        counts_mixed.insert(counts_mixed.end(), counts_points.begin() + counts_points.size() / 4, counts_points.begin() + counts_points.size() / 2);
        indices_mixed.insert(indices_mixed.end(), indices_points.begin() + indices_points.size() / 4, indices_points.begin() + indices_points.size() / 2);
        counts_mixed.insert(counts_mixed.end(), counts_poly.begin() + counts_poly.size() / 2, counts_poly.begin() + counts_poly.size());
        indices_mixed.insert(indices_mixed.end(), indices_poly.begin() + indices_poly.size() / 2, indices_poly.begin() + indices_poly.size());
    }


    aePolyMeshData mesh_data;
    mesh_data.points = (abcV3*)points.data();
    mesh_data.point_count = (int)points.size();
    mesh_data.uv0 = (abcV2*)uv.data();
    mesh_data.uv0_count = (int)uv.size();


    aeConfig config;
    config.frame_rate = 2.0f;
    config.scale_factor = 100.0f;

    auto ctx = aeCreateContext();
    aeSetConfig(ctx, &config);
    aeOpenArchive(ctx, "LinesAndPoints.abc");

    auto top = aeGetTopObject(ctx);
    {
        auto xf = aeNewXform(top, "Polygons");
        auto mesh = aeNewPolyMesh(xf, "Mesh");

        aeXformData xfd;
        xfd.translation.x = -0.8f;

        mesh_data.faces = counts_poly.data();
        mesh_data.face_count = (int)counts_poly.size();
        mesh_data.indices = indices_poly.data();
        mesh_data.index_count = (int)indices_poly.size();

        aeMarkFrameBegin(ctx);
        aeXformWriteSample(xf, &xfd);
        aePolyMeshWriteSample(mesh, &mesh_data);
        aeMarkFrameEnd(ctx);
    }
    {
        auto xf = aeNewXform(top, "Lines");
        auto mesh = aeNewPolyMesh(xf, "Mesh");

        aeXformData xfd;
        xfd.translation.x = 0.0f;

        mesh_data.faces = counts_lines.data();
        mesh_data.face_count = (int)counts_lines.size();
        mesh_data.indices = indices_lines.data();
        mesh_data.index_count = (int)indices_lines.size();

        aeMarkFrameBegin(ctx);
        aeXformWriteSample(xf, &xfd);
        aePolyMeshWriteSample(mesh, &mesh_data);
        aeMarkFrameEnd(ctx);
    }
    {
        auto xf = aeNewXform(top, "Points");
        auto mesh = aeNewPolyMesh(xf, "Mesh");

        aeXformData xfd;
        xfd.translation.x = 0.8f;

        mesh_data.faces = counts_points.data();
        mesh_data.face_count = (int)counts_points.size();
        mesh_data.indices = indices_points.data();
        mesh_data.index_count = (int)indices_points.size();

        aeMarkFrameBegin(ctx);
        aeXformWriteSample(xf, &xfd);
        aePolyMeshWriteSample(mesh, &mesh_data);
        aeMarkFrameEnd(ctx);
    }
    {
        auto xf = aeNewXform(top, "Mixed");
        auto mesh = aeNewPolyMesh(xf, "Mesh");

        aeXformData xfd;
        xfd.translation.z = 0.8f;

        mesh_data.faces = counts_mixed.data();
        mesh_data.face_count = (int)counts_mixed.size();
        mesh_data.indices = indices_mixed.data();
        mesh_data.index_count = (int)indices_mixed.size();

        aeMarkFrameBegin(ctx);
        aeXformWriteSample(xf, &xfd);
        aePolyMeshWriteSample(mesh, &mesh_data);
        aeMarkFrameEnd(ctx);
    }
    aeDestroyContext(ctx);
}
