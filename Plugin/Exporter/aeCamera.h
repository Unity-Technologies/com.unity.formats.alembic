#ifndef aeCamera_h
#define aeCamera_h

class aeCamera : public aeObject
{
typedef aeObject super;
public:
    aeCamera(aeObject *parent, const char *name);
    abcCamera& getAbcObject() override;
    abcProperties getAbcProperties() override;

    size_t  getNumSamples() override;
    void    setFromPrevious() override;
    void    writeSample(const aeCameraSampleData &data);

private:
    AbcGeom::OCameraSchema m_schema;
};

#endif // aeCamera_h
