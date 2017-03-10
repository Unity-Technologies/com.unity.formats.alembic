#pragma once

#ifdef _MSC_VER
    #define abciSTDCall __stdcall
    #pragma warning(disable: 4190)
#else // _MSC_VER
    #define abciSTDCall __attribute__((stdcall))
#endif // _MSC_VER

#ifdef abciImpl
    #ifndef abciStaticLink
        #ifdef _WIN32
            #define abciAPI extern "C" __declspec(dllexport)
        #else
            #define abciAPI extern "C"
        #endif
    #else
        #define abciAPI 
    #endif
#else
    #ifdef _MSC_VER
        #ifndef abciStaticLink
            #define abciAPI extern "C" __declspec(dllimport)
            #pragma comment(lib, "abci.lib")
        #else
            #define abciAPI extern "C"
            #pragma comment(lib, "abci_s.lib")
        #endif
    #else
    #endif

    struct abcV2
    {
        float x, y;

        abcV2() {}
        abcV2(float _x, float _y) : x(_x), y(_y) {}
    };

    struct abcV3
    {
        float x, y, z;

        abcV3() {}
        abcV3(float _x, float _y, float _z) : x(_x), y(_y), z(_z) {}
    };

    struct abcV4
    {
        float x, y, z, w;

        abcV4() {}
        abcV4(float _x, float _y, float _z, float _w) : x(_x), y(_y), w(_w) {}
    };

    struct abcSampleSelector
    {
        uint64_t m_requestedIndex;
        double m_requestedTime;
        int m_requestedTimeIndexType;
    };
#endif // abciImpl

#include "Importer/AlembicImporter.h"
#include "Exporter/AlembicExporter.h"
