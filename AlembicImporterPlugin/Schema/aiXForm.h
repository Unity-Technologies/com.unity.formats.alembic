#ifndef aiXForm_h
#define aiXForm_h


struct aiXFormData
{
    abcV3 translation;
    abcV4 rotation;
    abcV3 scale;
    uint8_t inherits;
};

class aiXFormSample : public aiSampleBase
{
typedef aiSampleBase super;
public:
    aiXFormSample(aiXForm *xf, float time);
    void getData(aiXFormData &o_data) const;

public:
    AbcGeom::XformSample m_sample;
    bool m_inherits;
};


struct aiXFormTraits
{
    typedef aiXFormSample SampleT;
    typedef AbcGeom::IXformSchema AbcSchemaT;
};

class aiXForm : public aiTSchema<aiXFormTraits>
{
typedef aiTSchema<aiXFormTraits> super;
public:
    aiXForm(aiObject *obj);
    Sample* readSample(float time) override;

private:
};

#endif // aiXForm_h
