#include "pch.h"
#include "Foundation.h"
#include "PartioPlugin.h"
#include "ppCache.h"

ppCache::ppCache(int num_particles)
{
    m_data = Partio::create();
    m_data->addParticles(num_particles);
}
ppCache::ppCache(const char *path)
{
    m_data = Partio::read(path);
}

ppCache::~ppCache()
{
    m_data->release();
}

ppCache::operator bool() const
{
    return m_data != nullptr;
}

bool ppCache::writeFile(const char *path)
{
    if (!m_data) return false;
    Partio::write(path, *m_data);
    return true;
}

int ppCache::getNumParticles()
{
    return m_data->numParticles();
}


int ppCache::getNumAttributes()
{
    return m_data->numAttributes();
}


static inline std::pair<Partio::ParticleAttributeType, int> ToPartioType(ppAttributeType type)
{
    int t = type & ppAttributeType_TypeMask;
    int c = type & ppAttributeType_CountMask;

    Partio::ParticleAttributeType pat = Partio::NONE;
    switch (t) {
    case ppAttributeType_FloatType:
        pat = c == 3 ? Partio::VECTOR : Partio::FLOAT;
        break;

    case ppAttributeType_IntType:
        pat = Partio::INT;
        break;
    }
    return std::make_pair(pat, c);
}

static inline ppAttributeType ToPPType(Partio::ParticleAttributeType t, int c)
{
    int tm = 0;
    if (t == Partio::FLOAT || t == Partio::VECTOR) tm = ppAttributeType_FloatType;
    else if (t == Partio::INT) tm = ppAttributeType_IntType;
    return ppAttributeType(tm | c);
}

static inline ppAttributeData ToAttributeData(Partio::ParticlesDataMutable *data, Partio::ParticleAttribute& attr)
{
    ppAttributeData ret;
    ret.type = ToPPType(attr.type, attr.count);
    ret.name = attr.name.c_str();
    ret.data = data->dataWrite<float>(attr, 0);
    return ret;
}


int ppCache::addAttribute(const char *name, ppAttributeType type)
{
    auto pt = ToPartioType(type);
    m_attributes.emplace_back(m_data->addAttribute(name, pt.first, pt.second));
    return (int)m_attributes.size() - 1;
}

ppAttributeData ppCache::getAttributeByID(int i)
{
    if (i >= m_attributes.size()) { return ppAttributeData(); }
    return ToAttributeData(m_data, m_attributes[i]);
}

ppAttributeData ppCache::getAttributeByName(const char *name)
{
    for (auto& attr : m_attributes) {
        if (attr.name == name) {
            return ToAttributeData(m_data, attr);
        }
    }
    return ppAttributeData();
}
