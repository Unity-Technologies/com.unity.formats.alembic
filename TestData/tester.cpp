#include <cstdlib>
#include <cstdio>
#include <cstdint>
#include <iostream>
#ifdef _WIN32
#include <windows.h>
#else
#include <dlfcn.h>
#endif

struct aiContext;
struct aiObject;
struct abcV2;
struct abcV3;

struct aiSubmeshInfo
{
    int index;
    int split_index;
    int split_submesh_index;
    int triangle_count;
    int faceset_index;
};

struct aiFacesets
{
    int count;
    int *face_counts;
    int *face_indices;
};

#ifdef _WIN32
typedef void (__stdcall *aiNodeEnumerator)(aiObject*, void*);
#else
typedef void (*aiNodeEnumerator)(aiObject*, void*);
#endif

typedef aiContext* (*aiCreateContextFunc)();
typedef void (*aiDestroyContextFunc)(aiContext*);

typedef bool (*aiLoadFunc)(aiContext*, const char*);
typedef float (*aiGetStartTimeFunc)(aiContext*);
typedef float (*aiGetEndTimeFunc)(aiContext*);
typedef aiObject* (*aiGetTopObjectFunc)(aiContext*);

typedef void (*aiEnumerateChildFunc)(aiObject*, aiNodeEnumerator, void*);
typedef const char* (*aiGetNameSFunc)(aiObject*);
typedef const char* (*aiGetFullNameSFunc)(aiObject*);
typedef uint32_t (*aiGetNumChildrenFunc)(aiObject*);
typedef void (*aiSetCurrentTimeFunc)(aiObject*, float);

typedef bool (*aiHasPolyMeshFunc)(aiObject*);
typedef bool (*aiPolyMeshHasNormalsFunc)(aiObject* obj);
typedef bool (*aiPolyMeshHasUVsFunc)(aiObject* obj);
typedef uint32_t (*aiPolyMeshGetSplitCountFunc)(aiObject* obj);
typedef uint32_t (*aiPolyMeshGetVertexBufferLengthFunc)(aiObject* obj, uint32_t split);
typedef void (*aiPolyMeshFillVertexBufferFunc)(aiObject* obj, uint32_t split, abcV3*, abcV3*, abcV2*);
typedef uint32_t (*aiPolyMeshPrepareSubmeshesFunc)(aiObject*, const aiFacesets*);
typedef uint32_t (*aiPolyMeshGetSplitSubmeshCountFunc)(aiObject*, uint32_t split);
typedef bool (*aiPolyMeshGetNextSubmeshFunc)(aiObject*, aiSubmeshInfo*);
typedef void (*aiPolyMeshFillSubmeshIndicesFunc)(aiObject*, int*, const aiSubmeshInfo*);

#ifdef _WIN32

typedef HMODULE Dso;

Dso LoadDso(const char *path)
{
    return LoadLibrary(path);
}

template <typename FuncType>
FuncType GetDsoSymbol(Dso dso, const char *symbol)
{
    return (FuncType) (dso != NULL ? GetProcAddress(dso, symbol) : 0);
}

void UnloadDso(Dso dso)
{
    if (dso != NULL) FreeLibrary(dso);
}

#else

typedef void* Dso;

Dso LoadDso(const char *path)
{
    return dlopen(path, RTLD_LAZY|RTLD_LOCAL);
}

template <typename FuncType>
FuncType GetDsoSymbol(Dso dso, const char *symbol)
{
    return (FuncType) (dso ? dlsym(dso, symbol) : 0);
}

void UnloadDso(Dso dso)
{
    if (dso) dlclose(dso);
}

#endif

struct API
{
    Dso dso;
    
    aiCreateContextFunc aiCreateContext;
    aiDestroyContextFunc aiDestroyContext;
    
    aiLoadFunc aiLoad;
    aiGetStartTimeFunc aiGetStartTime;
    aiGetEndTimeFunc aiGetEndTime;
    aiGetTopObjectFunc aiGetTopObject;
    
    aiEnumerateChildFunc aiEnumerateChild;
    aiGetNameSFunc aiGetName;
    aiGetFullNameSFunc aiGetFullName;
    aiGetNumChildrenFunc aiGetNumChildren;
    aiSetCurrentTimeFunc aiSetCurrentTime;
    
    aiHasPolyMeshFunc aiHasPolyMesh;
    aiPolyMeshHasNormalsFunc aiPolyMeshHasNormals;
    aiPolyMeshHasUVsFunc aiPolyMeshHasUVs;
    aiPolyMeshGetSplitCountFunc aiPolyMeshGetSplitCount;
    aiPolyMeshGetVertexBufferLengthFunc aiPolyMeshGetVertexBufferLength;
    aiPolyMeshFillVertexBufferFunc aiPolyMeshFillVertexBuffer;
    aiPolyMeshPrepareSubmeshesFunc aiPolyMeshPrepareSubmeshes;
    aiPolyMeshGetSplitSubmeshCountFunc aiPolyMeshGetSplitSubmeshCount;
    aiPolyMeshGetNextSubmeshFunc aiPolyMeshGetNextSubmesh;
    aiPolyMeshFillSubmeshIndicesFunc aiPolyMeshFillSubmeshIndices;
    
    API()
        : dso(0)
        , aiCreateContext(0)
        , aiDestroyContext(0)
        , aiLoad(0)
        , aiGetStartTime(0)
        , aiGetEndTime(0)
        , aiGetTopObject(0)
        , aiEnumerateChild(0)
        , aiGetName(0)
        , aiGetFullName(0)
        , aiSetCurrentTime(0)
        , aiHasPolyMesh(0)
        , aiPolyMeshHasNormals(0)
        , aiPolyMeshHasUVs(0)
        , aiPolyMeshGetSplitCount(0)
        , aiPolyMeshGetVertexBufferLength(0)
        , aiPolyMeshFillVertexBuffer(0)
        , aiPolyMeshPrepareSubmeshes(0)
        , aiPolyMeshGetSplitSubmeshCount(0)
        , aiPolyMeshGetNextSubmesh(0)
        , aiPolyMeshFillSubmeshIndices(0)
    {
    }
    
    API(const char *dsoPath)
    {
        dso = LoadDso(dsoPath);
        
        aiCreateContext = GetDsoSymbol<aiCreateContextFunc>(dso, "aiCreateContext");
        aiDestroyContext = GetDsoSymbol<aiDestroyContextFunc>(dso, "aiDestroyContext");
        
        aiLoad = GetDsoSymbol<aiLoadFunc>(dso, "aiLoad");
        aiGetStartTime = GetDsoSymbol<aiGetStartTimeFunc>(dso, "aiGetStartTime");
        aiGetEndTime = GetDsoSymbol<aiGetEndTimeFunc>(dso, "aiGetEndTime");
        aiGetTopObject = GetDsoSymbol<aiGetTopObjectFunc>(dso, "aiGetTopObject");
        
        aiEnumerateChild = GetDsoSymbol<aiEnumerateChildFunc>(dso, "aiEnumerateChild");
        aiGetName = GetDsoSymbol<aiGetNameSFunc>(dso, "aiGetNameS");
        aiGetFullName = GetDsoSymbol<aiGetFullNameSFunc>(dso, "aiGetFullNameS");
        aiGetNumChildren = GetDsoSymbol<aiGetNumChildrenFunc>(dso, "aiGetNumChildren");
        aiSetCurrentTime = GetDsoSymbol<aiSetCurrentTimeFunc>(dso, "aiSetCurrentTime");
        
        aiHasPolyMesh = GetDsoSymbol<aiHasPolyMeshFunc>(dso, "aiHasPolyMesh");
        aiPolyMeshHasNormals = GetDsoSymbol<aiPolyMeshHasNormalsFunc>(dso, "aiPolyMeshHasNormals");
        aiPolyMeshHasUVs = GetDsoSymbol<aiPolyMeshHasUVsFunc>(dso, "aiPolyMeshHasUVs");
        aiPolyMeshGetSplitCount = GetDsoSymbol<aiPolyMeshGetSplitCountFunc>(dso, "aiPolyMeshGetSplitCount");
        aiPolyMeshGetVertexBufferLength = GetDsoSymbol<aiPolyMeshGetVertexBufferLengthFunc>(dso, "aiPolyMeshGetVertexBufferLength");
        aiPolyMeshFillVertexBuffer = GetDsoSymbol<aiPolyMeshFillVertexBufferFunc>(dso, "aiPolyMeshFillVertexBuffer");
        aiPolyMeshPrepareSubmeshes = GetDsoSymbol<aiPolyMeshPrepareSubmeshesFunc>(dso, "aiPolyMeshPrepareSubmeshes");
        aiPolyMeshGetSplitSubmeshCount = GetDsoSymbol<aiPolyMeshGetSplitSubmeshCountFunc>(dso, "aiPolyMeshGetSplitSubmeshCount");
        aiPolyMeshGetNextSubmesh = GetDsoSymbol<aiPolyMeshGetNextSubmeshFunc>(dso, "aiPolyMeshGetNextSubmesh");
        aiPolyMeshFillSubmeshIndices = GetDsoSymbol<aiPolyMeshFillSubmeshIndicesFunc>(dso, "aiPolyMeshFillSubmeshIndices");
    }
    
    ~API()
    {
        UnloadDso(dso);
    }
};

struct EnumerateData
{
    API *api;
    aiContext *ctx;
    bool show_values;
    bool show_indices;
};

void EnumerateMesh(aiObject *obj, void *userdata)
{
    EnumerateData *data = (EnumerateData*) userdata;
    
    data->api->aiSetCurrentTime(obj, data->api->aiGetStartTime(data->ctx));
    
    if (data->api->aiHasPolyMesh(obj))
    {
        std::cout << "Found mesh: " << data->api->aiGetFullName(obj) << std::endl;
        
        uint32_t nsplits = data->api->aiPolyMeshGetSplitCount(obj);

        std::cout << "  " << nsplits << " split(s)" << std::endl;
        
        for (uint32_t s=0; s<nsplits; ++s)
        {
            size_t nv = data->api->aiPolyMeshGetVertexBufferLength(obj, s);
            std::cout << "  Split " << s << ": " << nv << " vertices" << std::endl;

            float *P = new float[3 * nv];
            float *N = (data->api->aiPolyMeshHasNormals(obj) ? new float[3 * nv] : 0);
            float *UV = (data->api->aiPolyMeshHasUVs(obj) ? new float[2 * nv] : 0);

            data->api->aiPolyMeshFillVertexBuffer(obj, s, (abcV3*)P, (abcV3*)N, (abcV2*)UV);

            if (data->show_values)
            {
                for (size_t v=0, v2=0, v3=0; v<nv; ++v, v2+=2, v3+=3)
                {
                    std::cout << "    " << v << ": P=(" << P[v3] << ", " << P[v3+1] << ", " << P[v3+2] << ")";
                    if (N)
                    {
                        std::cout << ", N=(" << N[v3] << ", " << N[v3+1] << ", " << N[v3+2] << ")";
                    }
                    if (UV)
                    {
                        std::cout << ", UV=(" << UV[v2] << ", " << UV[v2+1] << ")";
                    }
                    std::cout << std::endl;
                }
            }
        }

        uint32_t nsm = data->api->aiPolyMeshPrepareSubmeshes(obj, 0);

        std::cout << "  " << nsm << " submesh(es)" << std::endl;
        
        aiSubmeshInfo submesh;
        size_t i = 0;
        
        while (data->api->aiPolyMeshGetNextSubmesh(obj, &submesh))
        {
            std::cout << "  Submesh " << submesh.index << std::endl;
            
            std::cout << "    split: " << submesh.split_index << std::endl;
            std::cout << "    split submesh: " << submesh.split_submesh_index << " (" << (submesh.split_submesh_index + 1) << " of " << data->api->aiPolyMeshGetSplitSubmeshCount(obj, submesh.split_index) << ")" << std::endl;
            std::cout << "    faceset: " << submesh.faceset_index << std::endl;
            std::cout << "    triangles: " << submesh.triangle_count << std::endl;
            
            int *indices = new int[submesh.triangle_count * 3];
            
            data->api->aiPolyMeshFillSubmeshIndices(obj, indices, &submesh);

            if (data->show_indices)
            {
                int *index = indices;
                for (int j=0; j<submesh.triangle_count; ++j, index+=3)
                {
                    std::cout << "      " << j << ": " << index[0] << ", " << index[1] << ", " << index[2] << std::endl;
                }
            }

            ++i;
        }
    }
    else if (data->api->aiGetNumChildren(obj) > 0)
    {
        data->api->aiEnumerateChild(obj, EnumerateMesh, userdata);
    }
}

void usage()
{
    std::cout << "tester [OPTIONS] dsopath abcpath" << std::endl;
    std::cout << std::endl;
    std::cout << "OPTIONS" << std::endl;
    std::cout << "  -sv/--show-values  : Show vertex values" << std::endl;
    std::cout << "  -si/--show-indices : Show triangle indices" << std::endl;
    std::cout << "  -h/--help          : Show this help" << std::endl;
    std::cout << std::endl;
}

int main(int argc, char **argv)
{
    std::string dso;
    std::string abc;
    bool show_values = false;
    bool show_indices = false;

    // At least 3 arguments
    if (argc < 3)
    {
        usage();
        return 1;
    }

    for (int i=1; i<argc; ++i)
    {
        std::string arg = argv[i];
        
        if (arg == "-sv" || arg == "--show-values")
        {
            show_values = true;
        }
        else if (arg == "-si" || arg == "--show-indices")
        {
            show_indices = true;
        }
        else if (arg == "-h" || arg == "--help")
        {
            usage();
        }
        else if (dso.length() == 0)
        {
            dso = arg;
        }
        else if (abc.length() == 0)
        {
            abc = arg;
        }
        else
        {
            std::cout << "Unexpected argument: " << arg.c_str() << std::endl;
            std::cout << std::endl;
            usage();

            return 1;
        }
    }

    API api(dso.c_str());
    
    if (!api.dso)
    {
        std::cerr << "Invalid dso: " << dso.c_str() << std::endl;
        return 1;
    }

    int rv = 0;

    aiContext *ctx = api.aiCreateContext();
    
    if (ctx)
    {
        if (api.aiLoad(ctx, abc.c_str()))
        {
            float start = api.aiGetStartTime(ctx);
            float end = api.aiGetEndTime(ctx);
            
            std::cout << "Alembic time range: [" << start << ", " << end << "]" << std::endl;
            
            EnumerateData data = { &api, ctx, show_values, show_indices };
            
            api.aiEnumerateChild(api.aiGetTopObject(ctx), EnumerateMesh, (void*) &data);
        }
        else
        {
            std::cout << "Invalid alembic: " << abc.c_str() << std::endl;
            rv = 1;
        }
        
        api.aiDestroyContext(ctx);
    }
    else
    {
        std::cerr << "Could not create context" << std::endl;
        rv = 1;
    }
    
    return rv;
}

