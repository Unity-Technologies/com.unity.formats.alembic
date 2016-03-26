#include "pch.h"
#include "Foundation.h"
#include "PartioPlugin.h"
#include "ppCache.h"
#include "ppContext.h"


ppContext::ppContext()
{

}

ppContext::~ppContext()
{

}

size_t ppContext::getNumCaches() const
{
    return m_caches.size();
}
ppCache* ppContext::getCache(int i)
{
    return m_caches[i].get();
}
void ppContext::clearCache()
{
    m_caches.clear();
}
ppCache* ppContext::newCache()
{
    auto *ret = new ppCache();
    m_caches.push_back(ppCachePtr(ret));
    return ret;
}

bool ppContext::readFile(const char *path)
{
    auto *ret = new ppCache();
    if (!ret->read(path)) {
        delete ret;
        return false;
    }
    m_caches.push_back(ppCachePtr(ret));
    return true;
}
size_t ppContext::readFiles(const char *pattern)
{
    size_t ret = 0;
    tGlob(pattern, [&](const char *path) {
        if (readFile(path)) { ++ret; }
    });
    return ret;
}
ppIOAsync& ppContext::readFilesAsync(const char *pattern_)
{
    std::string pattern = pattern_;
    m_io_async = std::async(std::launch::async, [=]() {
        return readFiles(pattern.c_str());
    });
    return m_io_async;
}

bool ppContext::writeFile(const char *path, size_t nth)
{
    return m_caches[nth]->write(path);
}
size_t ppContext::writeFiles(const char *pattern)
{
    auto pair = tSplitFileExt(pattern);
    std::string path;
    char buf[64];

    size_t ret = 0;
    for (size_t i = 0; i < m_caches.size(); ++i) {
        sprintf(buf, "%04d", (int)i);
        path = pair.first;
        path += "_";
        path += buf;
        path += pair.second;
        if (writeFile(path.c_str(), i)) { ++ret; }
    }
    return ret;
}
ppIOAsync& ppContext::writeFilesAsync(const char *pattern_)
{
    std::string pattern = pattern_;
    m_io_async = std::async(std::launch::async, [=]() {
        return writeFiles(pattern.c_str());
    });
    return m_io_async;
}