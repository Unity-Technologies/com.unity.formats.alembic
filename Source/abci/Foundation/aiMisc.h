#pragma once

template<class IntType>
inline IntType ceildiv(IntType a, IntType b)
{
    return a / b + (a % b == 0 ? 0 : 1);
}
template<class IntType>
inline IntType ceilup(IntType a, IntType b)
{
    return ceildiv(a, b) * b;
}

template<class T>
inline T abcMin(const T& a, const T& b)
{
    return std::min(a, b);
}
template<class T>
inline T abcMax(const T& a, const T& b)
{
    return std::max(a, b);
}

template<>
inline abcV3 abcMin<abcV3>(const abcV3& a, const abcV3& b)
{
    return abcV3(std::min<float>(a.x, b.x), std::min<float>(a.y, b.y), std::min<float>(a.z, b.z));
}

template<>
inline abcV3 abcMax<abcV3>(const abcV3& a, const abcV3& b)
{
    return abcV3(std::max<float>(a.x, b.x), std::max<float>(a.y, b.y), std::max<float>(a.z, b.z));
}

template<class AbcPropertyType, class Body>
inline void abcEachSamples(AbcPropertyType& prop, const Body& body);

template<class AbcArrayPropertyType>
inline size_t abcArrayPropertyGetPeakSize(AbcArrayPropertyType& prop);

template<class AbcArrayPropertyType>
inline std::pair<
    typename AbcArrayPropertyType::value_type,
    typename AbcArrayPropertyType::value_type> abcArrayPropertyGetMinMaxValue(AbcArrayPropertyType& prop);

inline abcBoxd abcGetMaxBounds(const Abc::IBox3dProperty& prop);

template<class AbcPropertyType, class Body>
inline void abcEachSamples(AbcPropertyType& prop, const Body& body)
{
    size_t num_samples = prop.getNumSamples();
    for (size_t i = 0; i < num_samples; ++i)
    {
        body(prop.getValue(aiIndexToSampleSelector((int)i)));
    }
}

template<class AbcArrayPropertyType>
inline size_t abcArrayPropertyGetPeakSize(AbcArrayPropertyType& prop)
{
    Util::Dimensions dim;
    size_t num_samples = prop.getNumSamples();

    if (num_samples == 0)
    {
        return 0;
    }
    else if (prop.isConstant())
    {
        prop.getDimensions(dim, aiIndexToSampleSelector(0));
        return dim.numPoints();
    }
    else
    {
        size_t peak = 0;
        for (size_t i = 0; i < num_samples; ++i)
        {
            prop.getDimensions(dim, aiIndexToSampleSelector((int)i));
            peak = std::max<size_t>(peak, dim.numPoints());
        }
        return peak;
    }
}

template<class AbcArrayPropertyType>
inline std::pair<
    typename AbcArrayPropertyType::value_type,
    typename AbcArrayPropertyType::value_type> abcArrayPropertyGetMinMaxValue(AbcArrayPropertyType& prop)
{
    using value_type = typename AbcArrayPropertyType::value_type;
    using sample_ptr_type = typename AbcArrayPropertyType::sample_ptr_type;

    auto ret = std::make_pair(value_type(), value_type());

    if (!prop.valid())
    { return ret; }

    size_t num_samples = prop.getNumSamples();
    if (num_samples == 0)
    { return ret; }

    auto scan_minmax = [&](const value_type* value, size_t size)
    {
      for (size_t i = 0; i < size; ++i)
      {
          ret.first = abcMin<value_type>(ret.first, value[i]);
          ret.second = abcMax<value_type>(ret.second, value[i]);
      }
    };

    sample_ptr_type sample = prop.getValue(aiIndexToSampleSelector(0));

    bool first = true;
    if (sample->size())
    {
        ret.first = ret.second = sample->get()[0];
        first = false;
    }
    scan_minmax(sample->get(), sample->size());

    if (!prop.isConstant())
    {
        for (size_t i = 1; i < num_samples; ++i)
        {
            sample = prop.getValue(aiIndexToSampleSelector((int)i));
            if (first && sample->size())
            {
                ret.first = ret.second = sample->get()[0];
                first = false;
            }

            scan_minmax(sample->get(), sample->size());
        }
    }
    return ret;
}

inline abcBoxd abcGetMaxBounds(const Abc::IBox3dProperty& prop)
{
    abcV3 bmin, bmax;
    abcBoxd bounds;

    size_t n = prop.getNumSamples();
    if (n == 0)
    { return bounds; }

    prop.get(bounds, aiIndexToSampleSelector(0));
    bmin = (abcV3)bounds.min;
    bmax = (abcV3)bounds.max;

    for (size_t i = 1; i < n; ++i)
    {
        prop.get(bounds, aiIndexToSampleSelector((int)i));
        bmin = abcMin<abcV3>(bmin, (abcV3)bounds.min);
        bmax = abcMax<abcV3>(bmax, (abcV3)bounds.max);
    }

    return abcBoxd(bmin, bmax);
}
