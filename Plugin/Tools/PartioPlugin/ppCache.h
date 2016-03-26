#ifndef ppCache_h
#define ppCache_h

class ppCache
{
public:
    ppCache();
    ~ppCache();

    bool read(const char *path);
    bool write(const char *path);

private:
    Partio::ParticlesData *m_data;
};

#endif // ppCache_h
