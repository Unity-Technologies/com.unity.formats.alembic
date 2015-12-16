#ifndef aeXForm_h
#define aeXForm_h


struct aeXFormSampleData
{
    abcV3 translation;
    abcV4 rotation;
    abcV3 scale;
    bool inherits;

    inline aeXFormSampleData()
        : translation(0.0f, 0.0f, 0.0f)
        , rotation(0.0f, 0.0f, 0.0f, 1.0f)
        , scale(1.0f, 1.0f, 1.0f)
        , inherits(false)
    {
    }

    aeXFormSampleData(const aeXFormSampleData&) = default;
    aeXFormSampleData& operator=(const aeXFormSampleData&) = default;
};

class aeXForm : public aeSchemaBase
{
public:
    aeXForm(aeObject *obj);
    void writeSample(aeXFormSampleData &data);

private:
    AbcGeom::OXform m_abcobj;
    AbcGeom::OXformSchema m_schema;
};

#endif // aeXForm_h
