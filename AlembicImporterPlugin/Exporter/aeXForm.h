#ifndef aeXForm_h
#define aeXForm_h


struct aeXFormSampleData
{
    abcV3 translation;
    abcV3 rotation_axis;
    float rotation_angle;
    abcV3 scale;
    bool inherits;

    inline aeXFormSampleData()
        : translation(0.0f, 0.0f, 0.0f)
        , rotation_axis(0.0f, 0.0f, 0.0f)
        , rotation_angle(0.0f)
        , scale(1.0f, 1.0f, 1.0f)
        , inherits(true)
    {
    }

    aeXFormSampleData(const aeXFormSampleData&) = default;
    aeXFormSampleData& operator=(const aeXFormSampleData&) = default;
};

class aeXForm : public aeSchemaBase
{
typedef aeSchemaBase super;
public:
    aeXForm(aeObject *obj);
    void writeSample(const aeXFormSampleData &data);

private:
    AbcGeom::OXform m_abcobj;
    AbcGeom::OXformSchema m_schema;
    AbcGeom::XformSample m_sample;
};

#endif // aeXForm_h
