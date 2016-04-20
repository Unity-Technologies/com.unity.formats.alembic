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

ppCLinkage ppExport bool ppWriteFile(ppContext *ctx, const char *path, int i)
{
    if (!ctx) return false;
    return ctx->writeFile(path, i);
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

ppCLinkage ppExport void ppWait(ppIOAsync *async)
{
    if (async->valid()) {
        async->wait();
    }
}


ppCLinkage ppExport void ppClearCaches(ppContext *ctx)
{
    if (!ctx) return;
    ctx->clearCache();
}
ppCLinkage ppExport int ppGetNumCaches(ppContext *ctx)
{
    if (!ctx) return 0;
    return (int)ctx->getNumCaches();
}
ppCLinkage ppExport ppCache* ppGetCache(ppContext *ctx, int i)
{
    if (!ctx) return nullptr;
    return ctx->getCache(i);
}
ppCLinkage ppExport ppCache* ppAddCache(ppContext *ctx, int num_particles)
{
    if (!ctx) return nullptr;
    return ctx->addCache(num_particles);
}

ppCLinkage ppExport int ppGetNumParticles(ppCache *cache)
{
    if (!cache) return 0;
    return cache->getNumParticles();
}

ppCLinkage ppExport int ppGetNumAttributes(ppCache *cache)
{
    if (!cache) return 0;
    return cache->getNumAttributes();
}

ppCLinkage ppExport int ppAddAttribute(ppCache *cache, const char *name, ppAttributeType type)
{
    if (!cache) return 0;
    return cache->addAttribute(name, type);
}

ppCLinkage ppExport void ppGetAttributeDataByID(ppCache *cache, int i, ppAttributeData *dst)
{
    if (!cache || !dst) return;
    *dst = cache->getAttributeByID(i);
}

ppCLinkage ppExport void ppGetAttributeDataName(ppCache *cache, const char *name, ppAttributeData *dst)
{
    if (!cache || !dst) return;
    *dst = cache->getAttributeByName(name);
}


ppCLinkage ppExport void ppGetParticleData(ppCache *cache, ppCacheData *data)
{
    if (!cache || !data) return;
}
ppCLinkage ppExport void ppSetParticleData(ppCache *cache, ppCacheData *data)
{
    if (!cache || !data) return;
}
