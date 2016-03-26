#ifndef ppCache_h
#define ppCache_h

class ppCache
{
public:
    ppCache(int num_particles);
    ppCache(const char *path);
    ~ppCache();
    operator bool() const;

    bool writeFile(const char *path);

    int getNumParticles();

    int             getNumAttributes();
    int             addAttribute(const char *name, ppAttributeType type);
    ppAttributeData getAttributeByID(int i);
    ppAttributeData getAttributeByName(const char *name);

private:
    Partio::ParticlesDataMutable *m_data;
    std::vector<Partio::ParticleAttribute> m_attributes;
    std::vector<Partio::FixedAttribute> m_fattributes;
};

#endif // ppCache_h
