#ifndef aeXForm_h
#define aeXForm_h


class aeXForm : public aeObject
{
typedef aeObject super;
public:
    aeXForm(aeObject *parent, const char *name);
    AbcGeom::OXform& getAbcObject() override;

    void writeSample(const aeXFormSampleData &data);

private:
    AbcGeom::OXformSchema m_schema;
};

#endif // aeXForm_h
