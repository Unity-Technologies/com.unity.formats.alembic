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


int ppContext::getNumCaches() const
{
    return (int)m_caches.size();
}

ppCache* ppContext::getCache(int i)
{
    return m_caches[i].get();
}

void ppContext::clearCache()
{
    m_caches.clear();
}

ppCache* ppContext::addCache(int num_particles)
{
    auto *ret = new ppCache(num_particles);
    m_caches.push_back(ppCachePtr(ret));
    return ret;
}


bool ppContext::readFile(const char *path)
{
    auto *ret = new ppCache(path);
    if (!(*ret)) {
        delete ret;
        return false;
    }
    m_caches.push_back(ppCachePtr(ret));
    return true;
}

int ppContext::readFiles(const char *pattern)
{
    int ret = 0;
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


bool ppContext::writeFile(const char *path, int nth)
{
    return m_caches[nth]->writeFile(path);
}

int ppContext::writeFiles(const char *pattern)
{
    auto pair = tSplitFileExt(pattern);
    std::string path;
    char buf[64];

    int ret = 0;
    for (size_t i = 0; i < m_caches.size(); ++i) {
        // add index num to filename
        // e.g.: "hoge.geo" -> "hoge_0001.geo" ...
        sprintf(buf, "%04d", (int)i);
        path = pair.first;
        path += "_";
        path += buf;
        path += pair.second;

        if (writeFile(path.c_str(), (int)i)) { ++ret; }
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