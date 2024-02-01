#include "pch.h"
#include <Foundation/aiMath.h>
#include "aiCurves.h"
#include "aiUtils.h"
#include "Alembic/Abc/ITypedScalarProperty.h"


struct AttributeData
{
    void* data;
    void* samples1;
    void* samples2;
    void* ref;
    void* att = nullptr;
    void* att2;
    // att_interpolate is equivalent to rgb_int, uv_int ...etc.
    void* att_interpolate;
    void* constant_att = nullptr;
    RawVector<int> remap;
    int size;
    aiPropertyType type1;
    std::string name;
    bool interpolate = false;
    const Alembic::Abc::PropertyHeader& header;

    AttributeData(const Alembic::Abc::PropertyHeader& header) : header(header) {};
};


struct AttributeDataToTransfer
{
    int size;
    void* data;
    aiPropertyType type1;
};
aiCurvesSample::aiCurvesSample(aiCurves* schema) : super(schema)
{
}

void aiCurvesSample::getSummary(aiCurvesSampleSummary& dst)
{
    dst.positionCount = m_positions.size();
    dst.numVerticesCount = m_numVertices.size();
}

template<typename Tp>
void aiCurves::updateArbPropertySummaryAt(int paramIndex)
{
    auto& param = *static_cast<Tp*>(m_attributes_param[paramIndex]->data);

    if (param.valid() && param.getNumSamples() > 0 && param.getScope() != AbcGeom::kUnknownScope)
    {
        if (param.valid() && param.getNumSamples() > 0 && param.getScope() != AbcGeom::kUnknownScope)
        {
            // m_summary.attributeCount++;

            m_summary.has_attributes_prop.push_back(true);

            m_summary.constant_attributes->push_back(param.isConstant());

            m_summary.interpolate_attributes.push_back(false);
        }
    }
}

template<typename Tp, typename TpSample>
void aiCurves::readArbPropertySampleAt(int paramIndex, abcSampleSelector& ss, abcSampleSelector& ss2)
{
    auto attrib = m_attributes_param[paramIndex];
    auto* param = static_cast<Tp*>(attrib->data);

    attrib->samples1 = new TpSample; // otherwise dereference nullptr
    attrib->samples2 = new TpSample;

    TpSample* samp1 = static_cast<TpSample*>(attrib->samples1);
    TpSample* samp2 = static_cast<TpSample*>(attrib->samples2);

    param->getExpanded(*samp1, ss);

    if (attrib->interpolate)
    {
        param->getExpanded(*samp2, ss2);
    }
}

template<typename Tp, typename TpSample, typename VECTYPE>
void aiCurves::AssignArbPropertySampleAt(int paramIndex)
{
    auto attr = m_attributes_param[paramIndex];

    if (attr->att == nullptr) // otherwise risk to dereference nullptr
        attr->att = new RawVector<VECTYPE>;

    auto att_cast = static_cast<RawVector<VECTYPE>*>(attr->att);
    auto att_sp = *(static_cast<TpSample*>(attr->samples1));

    Assign(*att_cast, att_sp.getVals(), att_sp.getVals()->size());

    if (m_summary.interpolate_attributes[paramIndex])
    {
        if (attr->att2 == nullptr) // void* make it point to nullptr !
        {
            attr->att2 = new RawVector<VECTYPE>(); // otherwise null and crash
        }

        auto* att2_cast = static_cast<RawVector<VECTYPE>*>(attr->att2);
        auto att_sp2 = *(static_cast<TpSample*>(attr->samples2));

        Assign(*att2_cast, att_sp2.getVals(), att_sp2.getVals()->size());

        //Lerp(sample.m_widths.data(), sample.m_widths.data(), sample.m_widths2.data(),
        // sample.m_widths.size(), m_current_time_offset);
    }
}

template<typename TP, typename TpSample, typename VECTYPE>
void aiCurves::remapSecondAttributeSet(int paramIndex)
{
    auto param = m_attributes_param[paramIndex];

    if (param->att2 == nullptr) // otherwise risk to dereference nullptr
        param->att2 = new RawVector<VECTYPE>;

    auto att2_cast = static_cast<RawVector<VECTYPE>*>(param->att2);
    auto att_sp2 = *(static_cast<TpSample*>(param->samples2));

    Remap(*att2_cast, *att_sp2.getVals(), param->remap);
}

template<typename VECTYPE>
static inline void copy_or_clear_vector(int paramIndex, AttributeDataToTransfer dst[], const std::vector<AttributeData*>& src)
{
    auto ptrArray = new AttributeDataToTransfer();

    auto temp = static_cast<RawVector<VECTYPE>*>(src[paramIndex]->att);

    if (temp == nullptr)
        ptrArray[paramIndex].data = nullptr;
    else
        ptrArray[paramIndex].data = temp->data();

    ptrArray[paramIndex].type1 = src[paramIndex]->type1;
    ptrArray[paramIndex].size = sizeof(VECTYPE);

    memcpy(dst + paramIndex, ptrArray, sizeof(AttributeDataToTransfer));
};

template<>
static inline void copy_or_clear_vector<abcC3>(int paramIndex, AttributeDataToTransfer dst[], const std::vector<AttributeData*>& src)
{
    auto ptrArray = new AttributeDataToTransfer();

    auto temp = static_cast<RawVector<abcC3>*>(src[paramIndex]->att);

    if (temp == nullptr)
        ptrArray->data = nullptr;
    else
    {
        ptrArray->data = new abcC4[temp->size()];
        for (size_t j = 0; j < temp->size(); ++j)
        {
            abcC4* dataPtr = reinterpret_cast<abcC4*>(ptrArray[paramIndex].data);

            dataPtr[j].r = temp->data()[j].x;
            dataPtr[j].g = temp->data()[j].y;
            dataPtr[j].b = temp->data()[j].z;
            dataPtr[j].a = 1.0f;
        }
    }

    ptrArray[paramIndex].type1 = src[paramIndex]->type1;
    ptrArray[paramIndex].size = sizeof(abcC4);

    memcpy(dst + paramIndex, ptrArray, sizeof(AttributeDataToTransfer));
}

template<typename Tp>
void aiCurves::readAttribute(aiObject* object, std::vector<AttributeData*>& attributes)
{
    using abcGeomType = Tp;

    auto geom_params = this->m_schema.getArbGeomParams();

    if (geom_params.valid())
    {
        size_t num_geom_params = geom_params.getNumProperties();
        for (size_t i = 0; i < num_geom_params; ++i)
        {
            auto& header = geom_params.getPropertyHeader(i);
            if (abcGeomType::matches(header))
            {
                abcGeomType* param = new abcGeomType(geom_params, header.getName());

                // store samples ??
                size_t numSamples = param->getNumSamples();
                if (numSamples > 0)
                {
                    AttributeData* attribute = new AttributeData(header);
                    attribute->data = param; // param or samples ?
                    attribute->size = sizeof(param); // for now
                    attribute->type1 = aiGetPropertyType(header); // aiGetPropertyTypeID<abcGeomType> //
                    attribute->name = header.getName();
                    attributes.push_back(attribute);
                }
            }
        }
    }
}

// copyied
static aiPropertyType aiGetPropertyType(const Abc::PropertyHeader& header)
{
    const auto& dt = header.getDataType();

    if (header.getPropertyType() == Abc::kScalarProperty)
    {
        if (dt.getPod() == Abc::kBooleanPOD)
        {
            switch (dt.getNumBytes())
            {
                case 1: return aiPropertyType::Bool;
            }
        }
        else if (dt.getPod() == Abc::kInt32POD)
        {
            switch (dt.getNumBytes())
            {
                case 4: return aiPropertyType::Int;
            }
        }
        else if (dt.getPod() == Abc::kUint32POD)
        {
            switch (dt.getNumBytes())
            {
                case 4: return aiPropertyType::UInt;
            }
        }
        else if (dt.getPod() == Abc::kFloat32POD)
        {
            switch (dt.getNumBytes())
            {
                case 4: return aiPropertyType::Float;
                case 8: return aiPropertyType::Float2;
                case 12: return aiPropertyType::Float3;
                case 16: return aiPropertyType::Float4;
                case 64: return aiPropertyType::Float4x4;
            }
        }
    }
    else if (header.getPropertyType() == Abc::kArrayProperty)
    {
        if (dt.getPod() == Abc::kBooleanPOD)
        {
            switch (dt.getNumBytes())
            {
                case 1: return aiPropertyType::BoolArray;
            }
        }
        else if (dt.getPod() == Abc::kInt32POD)
        {
            switch (dt.getNumBytes())
            {
                case 4: return aiPropertyType::IntArray;
            }
        }
        else if (dt.getPod() == Abc::kUint32POD)
        {
            switch (dt.getNumBytes())
            {
                case 4: return aiPropertyType::UIntArray;
            }
        }
        else if (dt.getPod() == Abc::kFloat32POD)
        {
            switch (dt.getNumBytes())
            {
                case 4: return aiPropertyType::FloatArray;
                case 8: return aiPropertyType::Float2Array;
                case 12: return aiPropertyType::Float3Array;
                case 16: return aiPropertyType::Float4Array;
                case 64: return aiPropertyType::Float4x4Array;
            }
        }
    }
    return aiPropertyType::Unknown;
}

void aiCurvesSample::fillData(aiCurvesData& data)
{
    data.visibility = visibility;
    if (data.positions)
    {
        if (!m_positions.empty())
        {
            m_positions.copy_to(data.positions);
            m_numVertices.copy_to(data.numVertices);
            data.count = m_positions.size();
        }
    }

    if (data.uvs)
    {
        if (!m_uvs.empty())
            m_uvs.copy_to(data.uvs);
    }

    if (data.widths)
    {
        if (!m_widths.empty())
            m_widths.copy_to(data.widths);
    }

    if (data.velocities)
    {
        if (!m_velocities.empty())
            m_velocities.copy_to(data.velocities);
    }


    for (size_t i = 0; i < m_attributes_ref.size(); ++i)
    {
        auto attrib = m_attributes_ref[i];
        switch (attrib->type1)
        {
            //  case(aiPropertyType::BoolArray): copy_or_clear_vector<AbcGeom::IBoolGeomParam, int >(i, (AttributeDataToTransfer*)data.m_attributes, m_attributes_ref); break;
            case (aiPropertyType::IntArray): copy_or_clear_vector<int>(i, (AttributeDataToTransfer*)data.m_attributes, m_attributes_ref); break;
            case (aiPropertyType::UIntArray):  copy_or_clear_vector<unsigned int>(i, (AttributeDataToTransfer*)data.m_attributes, m_attributes_ref); break;
            case (aiPropertyType::FloatArray): copy_or_clear_vector<float>(i, (AttributeDataToTransfer*)data.m_attributes, m_attributes_ref); break;
            case (aiPropertyType::Float2Array): copy_or_clear_vector<abcV2>(i, (AttributeDataToTransfer*)data.m_attributes, m_attributes_ref); break;
            case (aiPropertyType::Float3Array):
            {
                if (AbcGeom::IV3fGeomParam::matches(attrib->header))
                    copy_or_clear_vector<abcV3>(i, (AttributeDataToTransfer*)data.m_attributes, m_attributes_ref);
                else if (AbcGeom::IC3fGeomParam::matches(attrib->header))
                    copy_or_clear_vector<abcC3>(i, (AttributeDataToTransfer*)data.m_attributes, m_attributes_ref);
                else if (AbcGeom::IN3fGeomParam::matches(attrib->header))
                    copy_or_clear_vector<abcV3>(i, (AttributeDataToTransfer*)data.m_attributes, m_attributes_ref);
                break;
            }
            case (aiPropertyType::Float4Array):
            {
                if (AbcGeom::IC4fGeomParam::matches(attrib->header))
                    copy_or_clear_vector<abcC4>(i, (AttributeDataToTransfer*)data.m_attributes, m_attributes_ref);
                break;
            }
            case (aiPropertyType::Float4x4): copy_or_clear_vector<abcM44d>(i, (AttributeDataToTransfer*)data.m_attributes, m_attributes_ref); break;
            default:
            case (aiPropertyType::Unknown): copy_or_clear_vector<AbcGeom::IV2fGeomParam::Sample>(i, (AttributeDataToTransfer*)data.m_attributes, m_attributes_ref); break;
        }
    }
}

aiCurves::aiCurves(aiObject* parent, const abcObject& abc) : super(parent, abc)
{
    readAttribute<AbcGeom::IInt32GeomParam>(parent, m_attributes_param);
    readAttribute<AbcGeom::IUInt32GeomParam>(parent, m_attributes_param);
    readAttribute<AbcGeom::IFloatGeomParam>(parent, m_attributes_param);
    readAttribute<AbcGeom::IV2fGeomParam>(parent, m_attributes_param);
    readAttribute<AbcGeom::IV3fGeomParam>(parent, m_attributes_param);
    readAttribute<AbcGeom::IC3fGeomParam>(parent, m_attributes_param);
    readAttribute<AbcGeom::IC4fGeomParam>(parent, m_attributes_param);
    readAttribute<AbcGeom::IM44fGeomParam>(parent, m_attributes_param);

    updateSummary();
}

aiCurvesSample* aiCurves::newSample()
{
    return new Sample(this);
}

void aiCurves::readSampleBody(aiCurvesSample& sample, uint64_t idx)
{
    auto ss = aiIndexToSampleSelector(idx);
    auto ss2 = aiIndexToSampleSelector(idx + 1);

    auto& summary = getSummary();
    bool interpolate = getConfig().interpolate_samples;


    bool topology_changed = m_varying_topology || this->m_force_update_local;

    //  auto& topology = *sample.m_topology;
    //   auto& refiner = topology.m_refiner;
    // if (topology_changed)
    //  topology.clear();

    // points
    if (summary.has_position)
    {
        {
            auto prop = m_schema.getPositionsProperty();
            prop.get(sample.m_position_sp, ss);
            if (interpolate)
            {
                prop.get(sample.m_position_sp2, ss2);
            }
        }
        {
            auto prop = m_schema.getNumVerticesProperty(); // Assume the same number
            prop.get(sample.m_numVertices_sp);
        }
    }

    if (summary.has_UVs)
    {
        auto prop = m_schema.getUVsParam();
        prop.getExpanded(sample.m_uvs_sp, ss);
        if (interpolate)
            prop.getExpanded(sample.m_uvs_sp2, ss2);
    }

    if (summary.has_widths)
    {
        auto prop = m_schema.getWidthsParam();
        prop.getExpanded(sample.m_widths_sp, ss);
        if (interpolate)
            prop.getExpanded(sample.m_widths_sp2, ss2);
    }

    if (summary.has_velocity)
    {
        auto prop = m_schema.getVelocitiesProperty();
        prop.get(sample.m_velocities_sp, ss);
    }


    for (size_t i = 0; i < m_attributes_param.size(); ++i)
    {
        auto attrib = (m_attributes_param)[i];

        if ((summary.has_attributes_prop)[i])
        {
            switch (attrib->type1)
            {
                case (aiPropertyType::BoolArray): this->readArbPropertySampleAt<AbcGeom::IBoolGeomParam, AbcGeom::IBoolGeomParam::Sample>(i, ss, ss2); break;
                case (aiPropertyType::IntArray): this->readArbPropertySampleAt<AbcGeom::IInt32GeomParam, AbcGeom::IInt32GeomParam::Sample>(i, ss, ss2); break;
                case (aiPropertyType::UIntArray): this->readArbPropertySampleAt<AbcGeom::IUInt32GeomParam, AbcGeom::IUInt32GeomParam::Sample>(i, ss, ss2); break;
                case (aiPropertyType::FloatArray): this->readArbPropertySampleAt<AbcGeom::IFloatGeomParam, AbcGeom::IFloatGeomParam::Sample>(i, ss, ss2); break;
                case (aiPropertyType::Float2Array): this->readArbPropertySampleAt<AbcGeom::IV2fGeomParam, AbcGeom::IV2fGeomParam::Sample>(i, ss, ss2); break;
                case (aiPropertyType::Float3Array):
                {
                    if (AbcGeom::IV3fGeomParam::matches(attrib->header))
                        this->readArbPropertySampleAt<AbcGeom::IV3fGeomParam, AbcGeom::IV3fGeomParam::Sample>(i, ss, ss2);
                    else if (AbcGeom::IC3fGeomParam::matches(attrib->header))
                        this->readArbPropertySampleAt<AbcGeom::IC3fGeomParam, AbcGeom::IC3fGeomParam::Sample>(i, ss, ss2);
                    else if (AbcGeom::IN3fGeomParam::matches(attrib->header))
                        this->readArbPropertySampleAt<AbcGeom::IN3fGeomParam, AbcGeom::IN3fGeomParam::Sample>(i, ss, ss2);
                    break;
                }
                case (aiPropertyType::Float4Array):
                {
                    if (AbcGeom::IC4fGeomParam::matches(m_attributes_param[i]->header))
                        this->readArbPropertySampleAt<AbcGeom::IC4fGeomParam, AbcGeom::IC4fGeomParam::Sample>(i, ss, ss2);
                    break;
                }
                case (aiPropertyType::Float4x4): this->readArbPropertySampleAt<AbcGeom::IM44dGeomParam, AbcGeom::IM44dGeomParam::Sample>(i, ss, ss2); break;
                default:
                case (aiPropertyType::Unknown): this->readArbPropertySampleAt<AbcGeom::IV2fGeomParam, AbcGeom::IV2fGeomParam::Sample>(i, ss, ss2); break;
            }
        }
    }
}

void aiCurves::cookSampleBody(aiCurvesSample& sample)
{
    auto& config = getConfig();
    int point_count = (int)sample.m_position_sp->size();
    bool interpolate = config.interpolate_samples;

    //if (!m_sample_index_changed)
    //  return;

    if (!getSummary().has_velocity)
        sample.m_positions.swap(sample.m_positions_prev);

    if (m_summary.has_position)
    {
        Assign(sample.m_numVertices, sample.m_numVertices_sp, sample.m_numVertices_sp->size());
        Assign(sample.m_positions, sample.m_position_sp, point_count);
        if (interpolate)
        {
            Assign(sample.m_positions2, sample.m_position_sp2, point_count);
            Lerp(sample.m_positions.data(), sample.m_positions.data(), sample.m_positions2.data(),
                point_count, m_current_time_offset);
        }
    }

    if (m_summary.has_UVs)
    {
        Assign(sample.m_uvs, sample.m_uvs_sp.getVals(), sample.m_uvs_sp.getVals()->size());
        if (interpolate)
        {
            Assign(sample.m_uvs2, sample.m_uvs_sp2.getVals(), sample.m_uvs_sp2.getVals()->size());
            Lerp(sample.m_uvs.data(), sample.m_uvs.data(), sample.m_uvs2.data(),
                sample.m_uvs.size(), m_current_time_offset);
        }
    }

    if (m_summary.has_widths)
    {
        Assign(sample.m_widths, sample.m_widths_sp.getVals(), sample.m_widths_sp.getVals()->size());
        if (interpolate)
        {
            Assign(sample.m_widths2, sample.m_widths_sp2.getVals(), sample.m_widths_sp2.getVals()->size());
            Lerp(sample.m_widths.data(), sample.m_widths.data(), sample.m_widths2.data(),
                sample.m_widths.size(), m_current_time_offset);
        }
    }

    if (config.swap_handedness)
    {
        SwapHandedness(sample.m_positions.data(), (int)sample.m_positions.size());
    }
    if (config.scale_factor != 1.0f)
    {
        ApplyScale(sample.m_positions.data(), (int)sample.m_positions.size(), config.scale_factor);
    }

    for (int i = 0; i < m_attributes_param.size(); i++)
    {
        auto attr = m_attributes_param[i];

        if (m_summary.has_attributes_prop[i])
        {
            {
                switch (attr->type1)
                {
                    //case(aiPropertyType::BoolArray): this->cookArbPropertySampleAt<AbcGeom::IBoolGeomParam, AbcGeom::IBoolGeomParam::Sample, bool>(i); break;
                    case (aiPropertyType::IntArray): this->AssignArbPropertySampleAt<AbcGeom::IInt32GeomParam, AbcGeom::IInt32GeomParam::Sample, int>(i); break;
                    case (aiPropertyType::UIntArray): this->AssignArbPropertySampleAt<AbcGeom::IUInt32GeomParam, AbcGeom::IUInt32GeomParam::Sample, unsigned int>(i); break;
                    case (aiPropertyType::FloatArray): this->AssignArbPropertySampleAt<AbcGeom::IFloatGeomParam, AbcGeom::IFloatGeomParam::Sample, float>(i); break;
                    case (aiPropertyType::Float2Array): this->AssignArbPropertySampleAt<AbcGeom::IV2fGeomParam, AbcGeom::IV2fGeomParam::Sample, abcV2>(i); break;
                    case (aiPropertyType::Float3Array):
                    {
                        if (AbcGeom::IV3dGeomParam::matches(attr->header))
                            this->AssignArbPropertySampleAt<AbcGeom::IV3fGeomParam, AbcGeom::IV3fGeomParam::Sample, abcV3>(i);
                        else if (AbcGeom::IC3fGeomParam::matches(attr->header))
                            this->AssignArbPropertySampleAt<AbcGeom::IC3fGeomParam, AbcGeom::IC3fGeomParam::Sample, abcC3>(i);
                        else if (AbcGeom::IN3fGeomParam::matches(attr->header))
                            this->AssignArbPropertySampleAt<AbcGeom::IN3fGeomParam, AbcGeom::IN3fGeomParam::Sample, abcV3>(i);
                        break;
                    }
                    case (aiPropertyType::Float4Array): this->AssignArbPropertySampleAt<AbcGeom::IC4fGeomParam, AbcGeom::IC4fGeomParam::Sample, abcC4>(i); break;
                    case (aiPropertyType::Float4x4): this->AssignArbPropertySampleAt<AbcGeom::IM44dGeomParam, AbcGeom::IM44dGeomParam::Sample, abcM44d>(i); break;
                    default:
                    case (aiPropertyType::Unknown): this->AssignArbPropertySampleAt<AbcGeom::IV2fGeomParam, AbcGeom::IV2fGeomParam::Sample, abcV2>(i); break;
                }
            }
        }

        sample.m_attributes_ref = m_attributes_param;
    }
    ;


    if (m_summary.has_velocity)
    {
        Assign(sample.m_velocities, sample.m_velocities_sp, point_count);
        if (config.swap_handedness)
        {
            SwapHandedness(sample.m_velocities.data(), (int)sample.m_velocities.size());
        }

        ApplyScale(sample.m_velocities.data(), (int)sample.m_velocities.size(), -config.scale_factor);
    }
    else
    {
        if (sample.m_positions_prev.empty())
            sample.m_velocities.resize_zeroclear(sample.m_positions.size());
        else
            GenerateVelocities(sample.m_velocities.data(), sample.m_positions.data(), sample.m_positions_prev.data(),
                (int)sample.m_positions.size(), -1 * config.vertex_motion_scale);
    }
}

void aiCurves::updateSummary()
{
    {
        auto prop = m_schema.getPositionsProperty();
        m_summary.has_position = prop.valid() && prop.getNumSamples() > 0;
    }
    {
        auto prop = m_schema.getUVsParam();
        m_summary.has_UVs = prop.valid() && prop.getNumSamples() > 0;
    }
    {
        auto prop = m_schema.getWidthsParam();
        m_summary.has_widths = prop.valid() && prop.getNumSamples() > 0;
    }
    {
        auto prop = m_schema.getVelocitiesProperty();
        m_summary.has_velocity = prop.valid() && prop.getNumSamples() > 0;
    }
    for (size_t i = 0; i < m_attributes_param.size(); ++i)
    {
        switch (m_attributes_param[i]->type1)
        {
            case (aiPropertyType::BoolArray): this->updateArbPropertySummaryAt<AbcGeom::IBoolGeomParam>(i); break;
            case (aiPropertyType::IntArray): this->updateArbPropertySummaryAt<AbcGeom::IInt32GeomParam>(i); break;
            case (aiPropertyType::UIntArray): this->updateArbPropertySummaryAt<AbcGeom::IUInt32GeomParam>(i); break;
            case (aiPropertyType::FloatArray): this->updateArbPropertySummaryAt<AbcGeom::IFloatGeomParam>(i); break;
            case (aiPropertyType::Float2Array): this->updateArbPropertySummaryAt<AbcGeom::IV2fGeomParam>(i); break;
            case (aiPropertyType::Float3Array):
            {
                if (AbcGeom::IV3fGeomParam::matches(m_attributes_param[i]->header))
                    this->updateArbPropertySummaryAt<AbcGeom::IV3fGeomParam>(i);
                else if (AbcGeom::IC3fGeomParam::matches(m_attributes_param[i]->header))
                    this->updateArbPropertySummaryAt<AbcGeom::IC3fGeomParam>(i);
                else if (AbcGeom::IN3fGeomParam::matches(m_attributes_param[i]->header))
                    this->updateArbPropertySummaryAt<AbcGeom::IN3fGeomParam>(i);
                break;
            }
            case (aiPropertyType::Float4Array):
            {
                if (AbcGeom::IC4fGeomParam::matches(m_attributes_param[i]->header))
                    this->updateArbPropertySummaryAt<AbcGeom::IC4fGeomParam>(i);
                break;
            }
            case (aiPropertyType::Float4x4): this->updateArbPropertySummaryAt<AbcGeom::IM44fGeomParam>(i); break;
            default:
            case (aiPropertyType::Unknown): this->updateArbPropertySummaryAt<AbcGeom::IV2fGeomParam>(i); break;
        }
        ;
    }

    //m_varying_topology = (this->m_schema.getTopologyVariance() == AbcGeom::kHeterogeneousTopology);
    auto& config = this->getConfig();
    this->m_constant = this->m_schema.isConstant();  // is this same as param ?
    bool interpolate = config.interpolate_samples && !this->m_constant;// && !m_varying_topology;

    if (interpolate)
    {
        for (int i = 0; i < m_summary.has_attributes_prop.size(); i++)
        {
            bool shouldInterpolate = !m_summary.has_attributes_prop.empty() &&
                m_summary.has_attributes_prop[i] && !(*(m_summary.constant_attributes))[i];

            m_summary.interpolate_attributes[i] = true;
        }
    }
}
