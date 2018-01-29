#pragma once

class aiPointsSample : public aiSampleBase
{
using super = aiSampleBase;
public:
    aiPointsSample(aiPoints *schema);
    virtual ~aiPointsSample();

    void getDataPointer(aiPointsData &data);
    void copyData(aiPointsData &data);

public:
    void createSortData();

    Abc::P3fArraySamplePtr m_points;
    Abc::V3fArraySamplePtr m_velocities;
    Abc::UInt64ArraySamplePtr m_ids;
    Abc::Box3d m_bounds;

    RawVector<std::pair<float, int>> m_sort_data;
    RawVector<abcV3> m_tmp_positions;
    RawVector<abcV3> m_tmp_velocities;
    RawVector<uint64_t> m_tmp_ids;
};


struct aiPointsTraits
{
    using SampleT = aiPointsSample;
    using AbcSchemaT = AbcGeom::IPointsSchema;
};

class aiPoints : public aiTSchema<aiPointsTraits>
{
using super = aiTSchema<aiPointsTraits>;
public:
    aiPoints(aiObject *obj);

    Sample* newSample();
    Sample* readSample(const uint64_t idx) override;

    const aiPointsSummary& getSummary() const;

    void setSort(bool v);
    bool getSort() const;
    void setSortPosition(const abcV3& v);
    const abcV3& getSortPosition() const;

private:
    mutable aiPointsSummary m_summary;
    bool m_sort = false;
    abcV3 m_sort_position = {0.0f, 0.0f, 0.0f};
};
