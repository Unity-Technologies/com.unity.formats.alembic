#ifndef aeXForm_h
#define aeXForm_h


class aeXForm : public aeObject
{
typedef aeObject super;
public:
    aeXForm(aeObject *parent, const char *name);
    abcXForm& getAbcObject() override;
    abcProperties getAbcProperties() override;

    void writeSample(const aeXFormSampleData &data);
    void setFromPrevious() override;

private:
    AbcGeom::OXformSchema m_schema;
    AbcGeom::XformSample m_sample;
};

#endif // aeXForm_h
