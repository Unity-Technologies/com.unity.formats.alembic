#pragma once

class aiXFormSample : public aiSampleBase
{
typedef aiSampleBase super;
public:
    aiXFormSample(aiXForm *schema);

    void updateConfig(const aiConfig &config, bool &topoChanged, bool &dataChanged) override;

    void getData(aiXFormData &outData) const;

public:
    AbcGeom::M44d m_matrix;
    AbcGeom::M44d m_nextMatrix;
    bool inherits;
private:
    void decomposeXForm(const Imath::M44d &mat, Imath::V3d &scale, Imath::V3d &shear, Imath::Quatd &rotation, Imath::V3d &translation) const;
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

    Sample* newSample();
    Sample* readSample(const uint64_t idx, bool &topologyChanged) override;
};
