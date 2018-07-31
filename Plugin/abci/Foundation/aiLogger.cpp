#include "pch.h"
#include <cstdarg>

void aiLogPrint(const char* fmt, ...)
{
    va_list vl;
    va_start(vl, fmt);
#ifdef _WIN32
    char buf[2048];
    vsprintf(buf, fmt, vl);
    ::OutputDebugStringA(buf);
    ::OutputDebugStringA("\n");
#else
    vprintf(fmt, vl);
#endif
    va_end(vl);
}

void aiLogPrint(const wchar_t* fmt, ...)
{
    va_list vl;
    va_start(vl, fmt);
#ifdef _WIN32
    wchar_t buf[2048];
    vswprintf(buf, fmt, vl);
    ::OutputDebugStringW(buf);
    ::OutputDebugStringW(L"\n");
#else
    vwprintf(fmt, vl);
#endif
    va_end(vl);
}
