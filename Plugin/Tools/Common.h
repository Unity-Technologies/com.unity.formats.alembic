#define tCLinkage extern "C"
#ifdef _MSC_VER
    #define tExport __declspec(dllexport)
#else
    #define tExport __attribute__((visibility("default")))
#endif
