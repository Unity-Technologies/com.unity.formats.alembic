#pragma once

class aiSampleBase;
class aiSchemaBase;


class aiSampleBase
{
public:
    aiSampleBase(aiSchemaBase *schema);
    virtual ~aiSampleBase();

    aiSchemaBase* getSchema() const { return m_schema; }

    // Note: data_changed MUST be true if topology_changed is
    virtual void updateConfig(const aiConfig &config, bool &data_changed) = 0;

public:
    AbcCoreAbstract::chrono_t m_current_time_offset = 0;
    AbcCoreAbstract::chrono_t m_current_time_interval = 0;
protected:
    aiSchemaBase *m_schema;
    aiConfig m_config;
};


class aiSchemaBase
{
public:
    using aiPropertyPtr = std::unique_ptr<aiProperty>;

    aiSchemaBase(aiObject *obj);
    virtual ~aiSchemaBase();

    aiObject* getObject() const;
    // config at last update time
    const aiConfig& getConfig() const;

    void setConfigCallback(aiConfigCallback cb, void *arg);
    void setSampleCallback(aiSampleCallback cb, void *arg);
    void invokeConfigCallback(aiConfig *config) const;
    void invokeSampleCallback(aiSampleBase *sample) const;
    virtual aiSampleBase*   updateSample(const abcSampleSelector& ss) = 0;

    void readConfig();

    bool isConstant() const;
    int getNumProperties() const;
    aiProperty* getPropertyByIndex(int i);
    aiProperty* getPropertyByName(const std::string& name);

protected:
    virtual abcProperties getAbcProperties() = 0;
    void setupProperties();
    void updateProperties(const abcSampleSelector& ss);

protected:
    aiObject *m_obj = nullptr;
    aiConfigCallback m_configCb = nullptr;
    void *m_configCbArg = nullptr;
    aiSampleCallback m_sampleCb = nullptr;
    void *m_sampleCbArg = nullptr;
    aiConfig m_config;
    bool m_constant = false;
    std::vector<aiPropertyPtr> m_properties; // sorted vector
};


template <class Traits>
class aiTSchema : public aiSchemaBase
{
public:
    using Sample = typename Traits::SampleT;
    using SamplePtr = std::shared_ptr<Sample>;
    using SampleCont = std::map<int64_t, SamplePtr>;
    using AbcSchema = typename Traits::AbcSchemaT;
    using AbcSchemaObject = Abc::ISchemaObject<AbcSchema>;


    aiTSchema(aiObject *obj)
        : aiSchemaBase(obj)
    {
        AbcSchemaObject abcObj(obj->getAbcObject(), Abc::kWrapExisting);
        m_schema = abcObj.getSchema();
        m_constant = m_schema.isConstant();
        m_time_sampling = m_schema.getTimeSampling();
        m_num_samples = static_cast<int64_t>(m_schema.getNumSamples());
        setupProperties();
    }

    virtual ~aiTSchema()
    {}

    int getTimeSamplingIndex() const
    {
        return m_obj->getContext()->getTimeSamplingIndex(m_schema.getTimeSampling());
    }

    int getSampleIndex(const abcSampleSelector& ss) const
    {
        return static_cast<int>(ss.getIndex(m_time_sampling, m_num_samples));
    }

    float getSampleTime(const abcSampleSelector& ss) const
    {
        return static_cast<float>(m_time_sampling->getSampleTime(ss.getRequestedIndex()));
    }

    int getNumSamples() const
    {
        return static_cast<int>(m_num_samples);
    }

    aiSampleBase* updateSample(const abcSampleSelector& ss) override;

protected:
    inline Sample* getSample()
    {
        return m_the_sample.get();
    }

    virtual Sample* readSample(const uint64_t idx) = 0;

    AbcGeom::ICompoundProperty getAbcProperties() override
    {
        return m_schema.getUserProperties();
    }

protected:
    AbcSchema m_schema;
    Abc::TimeSamplingPtr m_time_sampling;
    SamplePtr m_the_sample;
    int64_t m_num_samples = 0;
    int64_t m_last_sample_index = -1;
};

template<class Traits>
aiSampleBase* aiTSchema<Traits>::updateSample(const abcSampleSelector& ss)
{
    DebugLog("aiTSchema::updateSample()");

    readConfig();

    Sample* sample = nullptr;
    int64_t sample_index = getSampleIndex(ss);

    // don't need to check m_constant here, sampleIndex wouldn't change
    if (!m_the_sample || sample_index != m_last_sample_index) {
        DebugLog("  Read sample");
        sample = readSample(sample_index);
        if (sample != m_the_sample.get())
            m_the_sample.reset(sample);
    }
    else {
        DebugLog("  Update sample");
        bool data_changed = false;
        sample = m_the_sample.get();
        sample->updateConfig(m_config, data_changed);
        if (!m_config.force_update && !data_changed && !m_config.interpolate_samples) {
            DebugLog("  Data didn't change, nor update is forced");
            sample = nullptr;
        }
    }

    m_last_sample_index = sample_index;

    if (sample) {
        if (m_config.interpolate_samples) {
            AbcCoreAbstract::chrono_t interval = m_schema.getTimeSampling()->getTimeSamplingType().getTimePerCycle();
            auto index_time = m_time_sampling->getSampleTime(sample_index);
            sample->m_current_time_offset = std::max(0.0, std::min((ss.getRequestedTime() - index_time) / interval, 1.0));
            sample->m_current_time_interval = interval;
        }
        invokeSampleCallback(sample);
    }
    updateProperties(ss);

    return sample;
}
