#ifndef aiPoints_h
#define aiPoints_h


class aiPointsSample : public aiSampleBase
{
typedef aiSampleBase super;
public:
    aiPointsSample(aiPoints *schema);
    virtual ~aiPointsSample();

    void updateConfig(const aiConfig &config, bool &topoChanged, bool &dataChanged) override;
    void getDataPointer(aiPointsData &data);
    void copyData(aiPointsData &data);

public:
    Abc::P3fArraySamplePtr m_positions;
    Abc::V3fArraySamplePtr m_velocities;
    Abc::UInt64ArraySamplePtr m_ids;
    Abc::Box3d m_bounds;
};


struct aiPointsTraits
{
    typedef aiPointsSample SampleT;
    typedef AbcGeom::IPointsSchema AbcSchemaT;
};

class aiPoints : public aiTSchema<aiPointsTraits>
{
typedef aiTSchema<aiPointsTraits> super;
public:
    aiPoints(aiObject *obj);

    Sample* newSample();
    Sample* readSample(const abcSampleSelector& ss, bool &topologyChanged) override;

    int getPeakVertexCount() const;

private:
    mutable int m_peakVertexCount;
};

#endif
