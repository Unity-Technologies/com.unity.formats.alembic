#pragma once
#include <Foundation/AlignedVector.h>

struct aiPointsSummaryInternal : public aiPointsSummary
{
    bool interpolate_points = false;
    bool compute_velocities = false;
};


class aiPointsSample : public aiSample
{
    using super = aiSample;
public:
    aiPointsSample(aiPoints *schema);
    ~aiPointsSample();
    void fillData(aiPointsData &dst);
    void getSummary(aiPointsSampleSummary &dst);

public:
    Abc::P3fArraySamplePtr m_points_sp, m_points_sp2;
    Abc::V3fArraySamplePtr m_velocities_sp;
    Abc::UInt64ArraySamplePtr m_ids_sp;

    IArray<abcV3> m_points_ref;

    AlignedVector<std::pair<float, int> > m_sort_data;
    AlignedVector<abcV3> m_points, m_points2, m_points_int, m_points_prev;
    AlignedVector<abcV3> m_velocities;
    AlignedVector<uint32_t> m_ids;
    abcV3 m_bb_center, m_bb_size;
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
    aiPoints(aiObject *parent, const abcObject &abc);
    ~aiPoints();

    void updateSummary();
    const aiPointsSummaryInternal& getSummary() const;

    Sample* newSample() override;
    void readSampleBody(Sample& sample, uint64_t idx) override;
    void cookSampleBody(Sample& sample) override;

    void setSort(bool v);
    bool getSort() const;
    void setSortPosition(const abcV3& v);
    const abcV3& getSortPosition() const;

private:
    aiPointsSummaryInternal m_summary;
    bool m_sort = false;
    abcV3 m_sort_position = {0.0f, 0.0f, 0.0f};
};
