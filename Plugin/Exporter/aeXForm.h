#ifndef aeXForm_h
#define aeXForm_h


class aeXForm : public aeObject
{
typedef aeObject super;
public:
    aeXForm(aeObject *parent, const char *name);
    abcXForm& getAbcObject() override;
    abcProperties getAbcProperties() override;

    size_t  getNumSamples() override;
    void    setFromPrevious() override;
    void    writeSample(const aeXFormData &data);

private:
    AbcGeom::OXformSchema m_schema;
    AbcGeom::XformSample m_sample;
};

#endif // aeXForm_h
