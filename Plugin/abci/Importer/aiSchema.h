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
    virtual void updateConfig(const aiConfig &config, bool &topology_changed, bool &data_changed) = 0;

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
    void invokeSampleCallback(aiSampleBase *sample, bool topology_changed) const;
    virtual void cacheSamples(int64_t startIndex, int64_t endIndex)=0;
    virtual int             getTimeSamplingIndex() const = 0;
    virtual int             getNumSamples() const = 0;
    virtual aiSampleBase*   updateSample(const abcSampleSelector& ss) = 0;
    virtual aiSampleBase*   getSample(const abcSampleSelector& ss) const = 0;
    virtual int             getSampleIndex(const abcSampleSelector& ss) const = 0;
    virtual float           getSampleTime(const abcSampleSelector& ss) const = 0;

    void readConfig();
    bool hasVaryingTopology() const { return m_varying_topology; }

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
    bool m_varying_topology = false;
    std::vector<aiPropertyPtr> m_properties; // sorted vector
};


template <class Traits>
class aiTSchema : public aiSchemaBase
{
public:
    using Sample = typename Traits::SampleT;
    using SamplePtr = std::unique_ptr<Sample>;
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
    {
        if (dontUseCache() && m_the_sample) {
            delete m_the_sample;
        }
        m_samples.clear();
    }

    int getTimeSamplingIndex() const override
    {
        return m_obj->getContext()->getTimeSamplingIndex(m_schema.getTimeSampling());
    }

    int getSampleIndex(const abcSampleSelector& ss) const override
    {
        return static_cast<int>(ss.getIndex(m_time_sampling, m_num_samples));
    }

    float getSampleTime(const abcSampleSelector& ss) const override
    {
        return static_cast<float>(m_time_sampling->getSampleTime(ss.getRequestedIndex()));
    }

    int getNumSamples() const override
    {
        return static_cast<int>(m_num_samples);
    }

    void cacheSamples(int64_t startIndex, int64_t endIndex) override
    {
        if (m_constant) 
            return;
        if (startIndex == 0)
            readConfig();
        for (int64_t i = startIndex; i< endIndex; i++) {
            auto &sp = m_samples[i];   
            bool unused = i == 0;
            sp.reset(readSample(i, unused));
        }
    }

    aiSampleBase* updateSample(const abcSampleSelector& ss) override;

    Sample* getSample(const abcSampleSelector& ss) const override
    {
        DebugLog("aiTSchema::findSample(t=%f)", (float)ss.getRequestedTime());

        int sampleIndex = getSampleIndex(ss);
        if (dontUseCache()) {
            return (sampleIndex == m_last_sample_index ? m_the_sample : nullptr);
        }
        else {
            auto it = m_samples.find(sampleIndex);
            return (it != m_samples.end() ? it->second.get() : nullptr);
        }
    }

protected:

    inline bool useCache() const
    {
        return (!m_constant && m_config.cache_samples);
    }

    inline bool dontUseCache() const
    {
        return (m_constant || !m_config.cache_samples);
    }

    inline Sample* getSample()
    {
       if (dontUseCache() && m_the_sample) {
          return m_the_sample;
       }
       else {
          return nullptr;
       }
    }

    virtual Sample* readSample(const uint64_t idx, bool &topology_changed) = 0;
protected:
    AbcGeom::ICompoundProperty getAbcProperties() override
    {
        return m_schema.getUserProperties();
    }

protected:
    AbcSchema m_schema;
    SampleCont m_samples;
    Abc::TimeSamplingPtr m_time_sampling;
    Sample* m_the_sample = nullptr;
    int64_t m_num_samples = 0;
    int64_t m_last_sample_index = -1;
};

template<class Traits>
aiSampleBase* aiTSchema<Traits>::updateSample(const abcSampleSelector& ss)
{
    DebugLog("aiTSchema::updateSample()");

    readConfig();

    Sample* sample;
    bool topology_changed = false;
    int64_t sample_index = getSampleIndex(ss);

    if (dontUseCache()) {
        if (m_samples.size() > 0) {
            DebugLog("  Clear cached samples");

            m_samples.clear();
        }

        // don't need to check m_constant here, sampleIndex wouldn't change
        if (m_the_sample == 0 || sample_index != m_last_sample_index) {
            DebugLog("  Read sample");

            sample = readSample(sample_index, topology_changed);

            m_the_sample = sample;
        }
        else {
            DebugLog("  Update sample");

            bool data_changed = false;
            sample = m_the_sample;
            sample->updateConfig(m_config, topology_changed, data_changed);
            

            if (!m_config.force_update && !data_changed && !m_config.interpolate_samples) {
                DebugLog("  Data didn't change, nor update is forced");

                sample = 0;
            }
        }
    }
    else {
        auto& sp = m_samples[sample_index];

        if (!sp) {
            DebugLog("  Create new cache sample");

            sp.reset(readSample(sample_index, topology_changed));

            sample = sp.get();
        }
        else {
            DebugLog("  Update cached sample");

            bool data_changed = false;
            sample = sp.get();
            sample->updateConfig(m_config, topology_changed, data_changed);

            // force update if sample has changed from previously queried one
            if (sample_index != m_last_sample_index) {
                if (m_varying_topology) {
                    topology_changed = true;
                }
            }
            else if (!data_changed && !m_config.force_update && !m_config.interpolate_samples) {
                DebugLog("  Data didn't change, nor update is forced");

                sample = 0;
            }
        }
    }

    m_last_sample_index = sample_index;

    if (sample) {
        if (!m_varying_topology && m_config.interpolate_samples) {
            AbcCoreAbstract::chrono_t interval = m_schema.getTimeSampling()->getTimeSamplingType().getTimePerCycle();
            auto indexTime = m_time_sampling->getSampleTime(sample_index);
            sample->m_current_time_offset = std::max(0.0, std::min((ss.getRequestedTime() - indexTime) / interval, 1.0));
            sample->m_current_time_interval = interval;
        }
        invokeSampleCallback(sample, topology_changed);
    }
    updateProperties(ss);

    return sample;
}
