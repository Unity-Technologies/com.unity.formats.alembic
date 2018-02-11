#include "pch.h"
#include <cstdarg>

void aiLogPrint(const char* fmt, ...)
{
    char buf[2048];

    va_list vl;
    va_start(vl, fmt);
    vsprintf(buf, fmt, vl);
#ifdef _WIN32
    ::OutputDebugStringA(buf);
    ::OutputDebugStringA("\n");
#else
    puts(buf);
#endif
    va_end(vl);
}
