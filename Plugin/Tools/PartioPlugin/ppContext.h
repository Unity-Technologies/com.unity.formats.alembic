#ifndef ppContext_h
#define ppContext_h

class ppContext
{
public:
    ppContext();
    ~ppContext();

    int         getNumCaches() const;
    ppCache*    getCache(int i);
    void        clearCache();
    ppCache*    addCache(int num_particles);

    bool        readFile(const char *path);
    int         readFiles(const char *path);
    ppIOAsync&  readFilesAsync(const char *path);

    bool        writeFile(const char *path, int nth);
    int         writeFiles(const char *path);
    ppIOAsync&  writeFilesAsync(const char *path);

private:
    typedef std::shared_ptr<ppCache> ppCachePtr;

    std::vector<ppCachePtr> m_caches;
    ppIOAsync m_io_async;
};

#endif // ppContext_h
