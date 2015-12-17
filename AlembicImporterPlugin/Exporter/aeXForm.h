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
