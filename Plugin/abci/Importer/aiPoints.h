#pragma once
#include "aiIntrusiveArray.h"
#include "aiAsync.h"

class aiPointsSample : public aiSample
{
using super = aiSample;
public:
    aiPointsSample(aiPoints *schema);
    void fillData(aiPointsData &dst);
    void getSummary(aiPointsSampleSummary &dst);

    void sync() override;

public:
    Abc::P3fArraySamplePtr m_points_sp;
    Abc::V3fArraySamplePtr m_velocities_sp;
    Abc::UInt64ArraySamplePtr m_ids_sp;

    RawVector<std::pair<float, int>> m_sort_data;
    RawVector<abcV3> m_points;
    RawVector<abcV3> m_velocities;
    RawVector<uint32_t> m_ids;
    abcV3 m_bb_center, m_bb_size;

    std::future<void> m_async_copy;
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
    const aiPointsSummary& getSummary() const;

    Sample* newSample() override;
    void updateSample(const abcSampleSelector& ss) override;
    void readSample(Sample& sample, uint64_t idx) override;
    void cookSample(Sample& sample) override;
    void sync() override;

    void readSampleBody(Sample& sample, uint64_t idx);
    void cookSampleBody(Sample& sample);

    void setSort(bool v);
    bool getSort() const;
    void setSortPosition(const abcV3& v);
    const abcV3& getSortPosition() const;

private:
    mutable aiPointsSummary m_summary;
    bool m_sort = false;
    abcV3 m_sort_position = {0.0f, 0.0f, 0.0f};

    aiAsyncLoad m_async_load;
};
