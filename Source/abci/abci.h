#pragma once

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
        #define abciAPI extern "C"
    #endif

    struct abcV2 { float x, y; };
    struct abcV3 { float x, y, z; };
    struct abcV4 { float x, y, z, w; };
    using abcC4 = abcV4;

    struct abcSampleSelector
    {
        uint64_t m_requestedIndex;
        double m_requestedTime;
        int m_requestedTimeIndexType;
    };
#endif // abciImpl

#include "Importer/AlembicImporter.h"
#include "Exporter/AlembicExporter.h"
