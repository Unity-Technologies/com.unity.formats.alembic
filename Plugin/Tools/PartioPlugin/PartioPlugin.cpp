#include "pch.h"
#include "Foundation.h"
#include "PartioPlugin.h"
#include "ppCache.h"
#include "ppContext.h"


ppCLinkage ppExport ppContext* ppCreateContext(const ppConfig *conf)
{
    return new ppContext();
}
ppCLinkage ppExport void ppDestroyContext(ppContext *ctx)
{
    delete ctx;
}

ppCLinkage ppExport bool ppReadFile(ppContext *ctx, const char *path)
{
    if (!ctx) return false;
    return ctx->readFile(path);
}
ppCLinkage ppExport size_t ppReadFiles(ppContext *ctx, const char *path)
{
    if (!ctx) return 0;
    return ctx->readFiles(path);
}
ppCLinkage ppExport ppIOAsync* ppReadFilesAsync(ppContext *ctx, const char *path)
{
    if (!ctx) return nullptr;
    return &ctx->readFilesAsync(path);
}

ppCLinkage ppExport bool ppWriteFile(ppContext *ctx, const char *path, int nth)
{
    if (!ctx) return false;
    return ctx->writeFile(path, nth);
}
ppCLinkage ppExport size_t ppWriteFiles(ppContext *ctx, const char *path)
{
    if (!ctx) return 0;
    return ctx->writeFiles(path);
}
ppCLinkage ppExport ppIOAsync* ppWriteFilesAsync(ppContext *ctx, const char *path)
{
    if (!ctx) return nullptr;
    return &ctx->writeFilesAsync(path);
}
