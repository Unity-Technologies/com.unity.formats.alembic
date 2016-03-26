#ifndef ppContext_h
#define ppContext_h

class ppContext
{
public:
    ppContext();
    ~ppContext();

    size_t      getNumCaches() const;
    ppCache*    getCache(int i);
    void        clearCache();
    ppCache*    newCache();

    bool        readFile(const char *path);
    size_t      readFiles(const char *path);
    ppIOAsync&  readFilesAsync(const char *path);

    bool        writeFile(const char *path, size_t nth);
    size_t      writeFiles(const char *path);
    ppIOAsync&  writeFilesAsync(const char *path);

private:
    typedef std::shared_ptr<ppCache> ppCachePtr;

    std::vector<ppCachePtr> m_caches;
    ppIOAsync m_io_async;
};

#endif // ppContext_h
