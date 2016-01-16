#ifndef aiPoints_h
#define aiPoints_h


class aiPointsSample : public aiSampleBase
{
typedef aiSampleBase super;
public:
    aiPointsSample(aiPoints *schema);
    virtual ~aiPointsSample();

    void updateConfig(const aiConfig &config, bool &topoChanged, bool &dataChanged) override;
    void getDataPointer(aiPointsSampleData &data);
    void copyData(aiPointsSampleData &data);

public:
    Abc::P3fArraySamplePtr m_positions;
    Abc::V3fArraySamplePtr m_velocities;
    Abc::UInt64ArraySamplePtr m_ids;
    Abc::Box3d m_bounds;
};

struct aiPointsSampleData
{
    abcV3 *positions;
    abcV3 *velocities;
    uint64_t *ids;
    abcV3 boundsCenter;
    abcV3 boundsExtents;
    int32_t count;

    inline aiPointsSampleData()
        : positions(nullptr)
        , velocities(nullptr)
        , ids(nullptr)
        , count(0)
    {
    }

    aiPointsSampleData(const aiPointsSampleData&) = default;
    aiPointsSampleData& operator=(const aiPointsSampleData&) = default;
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
