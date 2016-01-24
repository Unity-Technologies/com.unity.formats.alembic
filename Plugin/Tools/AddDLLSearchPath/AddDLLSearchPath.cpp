#include "pch.h"
#include "Foundation.h"

tCLinkage tExport const char* GetDirectoryOfCurrentModule()
{
    return tGetDirectoryOfCurrentModule();
}

tCLinkage tExport void AddDLLSearchPath(const char *path)
{
    if (path == nullptr) {
        path = tGetDirectoryOfCurrentModule();
    }
    tAddDLLSearchPath(path);
}
