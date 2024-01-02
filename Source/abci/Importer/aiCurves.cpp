#include "pch.h"
#include <Foundation/aiMath.h>
#include "aiCurves.h"
#include "aiUtils.h"
#include "Alembic/Abc/ITypedScalarProperty.h"


struct AttributeData {
    void* data;
    void* samples1;
    void* samples2;
    void* ref;
    void* att = nullptr;
    void* att2;
    void* att_int;
    void* constant_att = nullptr;
    RawVector<int> remap;
    int size;
    aiPropertyType type1;
    std::string name;
    bool interpolate = false;
};

//template <typename TP>
struct AttributeDataToTransfer {
    int size;
    //using Typ = TP
    void* data;
    // std::string name;
    aiPropertyType type1;
    // std::string type2;
};
aiCurvesSample::aiCurvesSample(aiCurves *schema) : super(schema)
{
}

void aiCurvesSample::getSummary(aiCurvesSampleSummary &dst)
{
    dst.positionCount = m_positions.size();
    dst.numVerticesCount = m_numVertices.size();
}

static inline void copy_or_clear_vector(AttributeDataToTransfer dst[], const std::vector<AttributeData*>* src)
{

    auto ptrArray = new AttributeDataToTransfer[11];
    for (int i = 0; i < src->size(); i++) {
        AttributeDataToTransfer a();

        switch ((*src)[i]->type1) {

        case(aiPropertyType::Unknown):
        {
            auto temp = static_cast<RawVector<abcV2>*>((*src)[i]->data);

            if (temp == nullptr)
                ptrArray[i].data = nullptr;
            else
                ptrArray[i].data = temp->data();

            ptrArray[i].type1 = (*src)[i]->type1;
            ptrArray[i].size = sizeof(abcV2);

            break;
        }


        case(aiPropertyType::Float):
        {
            auto temp = static_cast<RawVector<float>*>((*src)[i]->data);

            if (temp == nullptr)
                ptrArray[i].data = nullptr;
            else
                ptrArray[i].data = temp->data();

            ptrArray[i].type1 = (*src)[i]->type1;
            ptrArray[i].size = sizeof(float);

            break;
        }


        case(aiPropertyType::Float3Array):
        {
            auto temp = static_cast<RawVector<abcC3>*>((*src)[i]->att);
            if (temp == nullptr)
                ptrArray[i].data = nullptr;
            else
            {
                //ptrArray[i].data = (abcC4*)temp->data();

                ptrArray[i].data = new abcC4[temp->size()];
                for (size_t j = 0; j < temp->size(); ++j)
                {
                    abcC4* dataPtr = reinterpret_cast<abcC4*>(ptrArray[i].data);

                    dataPtr[j].r = temp->data()[j].x;
                    dataPtr[j].g = temp->data()[j].y;
                    dataPtr[j].b = temp->data()[j].z;
                    dataPtr[j].a = 1.0f;
                }
            }
            ptrArray[i].type1 = (*src)[i]->type1;
            ptrArray[i].size = sizeof(abcC3);

            break; }
        }
    }


  memcpy(dst, ptrArray, sizeof(AttributeDataToTransfer) * src->size());

};

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

  copy_or_clear_vector((AttributeDataToTransfer*)data.m_attributes, m_attributes_ref);

    // copy_or_clear
}




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

template <typename Tp>
void aiCurves::ReadAttribute(aiObject* object, std::vector<AttributeData*>* attributes)
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
                  

                    AttributeData* attribute = new AttributeData();
                    attribute->data = param; // param or samples ? 
                    attribute->size = sizeof(param); // for now 
                    attribute->type1 = aiGetPropertyType(header); // aiGetPropertyTypeID<abcGeomType> //
                    attribute->name = header.getName();
                    attributes->push_back(attribute);
                }
            }

        }
    }

}


aiCurves::aiCurves(aiObject *parent, const abcObject &abc) : super(parent, abc)
{

    ReadAttribute<AbcGeom::IC3fGeomParam>(parent, m_attributes_param);
    ReadAttribute<AbcGeom::IV3fGeomParam>(parent, m_attributes_param);
    ReadAttribute<AbcGeom::ITypedGeomParam<Abc::V2fTPTraits>>(parent, m_attributes_param);
    ReadAttribute<AbcGeom::IV2fGeomParam>(parent, m_attributes_param);
    ReadAttribute<AbcGeom::IV2fProperty>(parent, m_attributes_param);
    ReadAttribute<AbcGeom::IFloatProperty>(parent, m_attributes_param);
    ReadAttribute < AbcGeom::IFloatGeomParam > (parent, m_attributes_param);
    updateSummary();
}

aiCurvesSample *aiCurves::newSample()
{
    return new Sample(this);
}


void aiCurves::readSampleBody(aiCurvesSample &sample, uint64_t idx)
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


    for (size_t i = 0; i < m_attributes_param->size(); ++i) {
        if (summary.has_attributes->at(i)) {
            auto* attrib = (*m_attributes_param)[i];
            auto* param = static_cast<AbcGeom::IC3fGeomParam*>(attrib->data);

            attrib->samples1 = new AbcGeom::IC3fGeomParam::Sample; // otherwise dereference nullptr 
            attrib->samples2 = new AbcGeom::IC3fGeomParam::Sample;
   
            AbcGeom::IC3fGeomParam::Sample* samp1 = static_cast<AbcGeom::IC3fGeomParam::Sample*>(attrib->samples1);
            AbcGeom::IC3fGeomParam::Sample* samp2 = static_cast<AbcGeom::IC3fGeomParam::Sample*> (attrib->samples2);
            param->getExpanded(*samp1, ss);// or getindexed???

            if (attrib->interpolate) {
                param->getExpanded(*samp2, ss2);
            }
        }

       
    }
}

void aiCurves::cookSampleBody(aiCurvesSample &sample)
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

    //ONTOPOLOGYCHANGE ??
    for (int i = 0; i < (m_attributes_param)->size(); i++) {
     
      

            if ((((*m_attributes_param)[i])->constant_att) != nullptr)
            {
                std::cout << "const";
            }
            else if (m_summary.has_attributes->at(i))
            {

                if ((*(m_attributes_param))[i]->att == nullptr) // void* make it point to nullptr ! 
                {
                    (*(m_attributes_param))[i]->att = new RawVector<abcC3>(); // otherwise null and crash
                }
                auto* att1_cast = static_cast<RawVector<abcC3>*>((*m_attributes_param)[i]->att);
                auto att_sp1 = *(static_cast<AbcGeom::IC3fGeomParam::Sample*>((*m_attributes_param)[i]->samples1));
                Assign(*att1_cast, att_sp1.getVals(), att_sp1.getVals()->size());

                if ((*m_attributes_param)[i]->interpolate) {
                    if ((*(m_attributes_param))[i]->att2 == nullptr) // void* make it point to nullptr ! 
                    {
                        (*(m_attributes_param))[i]->att2 = new RawVector<abcC3>(); // otherwise null and crash
                    }
                    auto* att2_cast = static_cast<RawVector<abcC3>*>((*m_attributes_param)[i]->att2);
                    auto att_sp2 = *(static_cast<AbcGeom::IC3fGeomParam::Sample*>((*m_attributes_param)[i]->samples2));
                    Assign(*att2_cast, att_sp2.getVals(), att_sp2.getVals()->size());

                    //Lerp(sample.m_widths.data(), sample.m_widths.data(), sample.m_widths2.data(),
                       // sample.m_widths.size(), m_current_time_offset);
                }

                sample.m_attributes_ref = m_attributes_param; 
            }

        
    };


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
   for (size_t i = 0; i < m_attributes_param->size(); ++i) {
     
           auto& param = *static_cast<AbcGeom::IC3fGeomParam*>((*m_attributes_param)[i]->data);

           if (param.valid() && param.getNumSamples() > 0 && param.getScope() != AbcGeom::kUnknownScope) {
               m_summary.has_attributes->push_back(true);
               
               m_summary.has_attributes_prop->push_back(true);

               m_summary.constant_attribute->push_back(param.isConstant());
           }
      
       }

   //m_varying_topology = (this->m_schema.getTopologyVariance() == AbcGeom::kHeterogeneousTopology);
   auto& config = this->getConfig();
   this->m_constant = this->m_schema.isConstant();
   bool interpolate = config.interpolate_samples && !this->m_constant;// && !m_varying_topology;
   //bool interpolate = config.interpolate_samples && !this->m_constant && !m_varying_topology;
   if (interpolate) {
       for (int i = 0; i < m_summary.has_attributes->size(); i++)
       {
           if (!m_summary.has_attributes_prop->empty())
           {
               if ((*m_summary.has_attributes_prop)[i] && !((*m_summary.constant_attributes)[i]))
               {

                   (*m_summary.interpolate_attributes).push_back(true);

               }
               else { (*m_summary.interpolate_attributes).push_back(false); };
           }
           else { (*m_summary.interpolate_attributes).push_back(false); };
       };
   }

   /*
          for (int i = 0; i < summary.has_attributes -> size(); i++)
        {
            if (!summary.has_attributes_prop->empty())
            {
                if ((*summary.has_attributes_prop)[i] && !((*summary.constant_attributes)[i]))
                {

                    (*summary.interpolate_attributes).push_back(true);

                }
                else { (*summary.interpolate_attributes).push_back(false); };
            }
            else { (*summary.interpolate_attributes).push_back(false); };
        };*/

        /*
        case(aiPropertyType::Float2Array):
        {
            auto& param = *static_cast<AbcGeom::IV2fGeomParam*>((*m_attributes_param)[i]->data);

            if (param.valid() && param.getNumSamples() > 0 && param.getScope() != AbcGeom::kUnknownScope) {
                m_summary.has_attributes_prop->push_back(true);
                m_summary.has_attributes->push_back(true);

                m_summary.constant_attributes->push_back(param.isConstant());
            }
            break;
        };


        case(aiPropertyType::Float3Array):
        {
            auto& param = *static_cast<AbcGeom::IC3fGeomParam*>((*m_attributes_param)[i]->data);

            if (param.valid() && param.getNumSamples() > 0 && param.getScope() != AbcGeom::kUnknownScope) {
                m_summary.has_attributes_prop->push_back(true);
                m_summary.has_attributes->push_back(true);

                m_summary.constant_attributes->push_back(param.isConstant());
            }
            break;
        };
        };
    }*/

}
