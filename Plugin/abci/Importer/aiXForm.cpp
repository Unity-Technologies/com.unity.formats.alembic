#include "pch.h"
#include "aiInternal.h"
#include "aiContext.h"
#include "aiObject.h"
#include "aiSchema.h"
#include "aiXForm.h"


aiXformSample::aiXformSample(aiXform *schema)
    : super(schema), inherits(true)
{
}

void aiXformSample::getData(aiXformData &dst) const
{
    dst = m_data;
}


aiXform::aiXform(aiObject *parent, const abcObject &abc)
    : super(parent, abc)
{
}

aiXform::Sample* aiXform::newSample()
{
    return new Sample(this);
}

void aiXform::updateSample(const abcSampleSelector& ss)
{
    m_async_load.reset();

    super::updateSample(ss);
    if (m_async_load.ready())
        getContext()->queueAsync(m_async_load);
}

void aiXform::readSample(Sample& sample, uint64_t idx)
{
    auto body = [this, &sample, idx]() {
        readSampleBody(sample, idx);
    };

    if (m_force_sync || !getConfig().async_load)
        body();
    else
        m_async_load.m_read = body;
}

void aiXform::cookSample(Sample& sample)
{
    auto body = [this, &sample]() {
        cookSampleBody(sample);
    };

    if (m_force_sync || !getConfig().async_load)
        body();
    else
        m_async_load.m_cook = body;
}

void aiXform::readSampleBody(Sample& sample, uint64_t idx)
{
    auto ss = aiIndexToSampleSelector(idx);
    auto ss2 = aiIndexToSampleSelector(idx + 1);
    readVisibility(sample, ss);

    AbcGeom::XformSample xf_sample;
    m_schema.get(xf_sample, ss);
    sample.m_matrix = xf_sample.getMatrix();
    sample.inherits = xf_sample.getInheritsXforms();

    AbcGeom::XformSample xf_sample2;
    m_schema.get(xf_sample2, ss2);
    sample.m_next_matrix = xf_sample2.getMatrix();
}

void aiXform::cookSampleBody(Sample& sample)
{
    auto& config = getConfig();

    Imath::V3d scale;
    Imath::V3d shear;
    Imath::Quatd rot;
    Imath::V3d trans;
    decompose(sample.m_matrix, scale, shear, rot, trans);

    if (config.interpolate_samples && m_current_time_offset != 0)
    {
        Imath::V3d scale2;
        Imath::Quatd rot2;
        Imath::V3d trans2;
        decompose(sample.m_next_matrix, scale2, shear, rot2, trans2);
        scale += (scale2 - scale)* m_current_time_offset;
        trans += (trans2 - trans)* m_current_time_offset;
        rot = Imath::slerpShortestArc(rot, rot2, (double)m_current_time_offset);
    }

    auto rot_final = abcV4(
        static_cast<float>(rot.v[0]),
        static_cast<float>(rot.v[1]),
        static_cast<float>(rot.v[2]),
        static_cast<float>(rot.r)
    );

    if (config.swap_handedness)
    {
        trans.x *= -1.0f;
        rot_final.x = -rot_final.x;
        rot_final.w = -rot_final.w;
    }
    auto& dst = sample.m_data;
    dst.visibility = sample.visibility;
    dst.inherits = sample.inherits;
    dst.translation = trans;
    dst.rotation = rot_final;
    dst.scale = scale;
}

void aiXform::decompose(const Imath::M44d &mat, Imath::V3d &scale, Imath::V3d &shear, Imath::Quatd &rotation, Imath::V3d &translation) const
{
    Imath::M44d mat_remainder(mat);

    // Extract Scale, Shear
    Imath::extractAndRemoveScalingAndShear(mat_remainder, scale, shear, false);

    // Extract translation
    translation.x = mat_remainder[3][0];
    translation.y = mat_remainder[3][1];
    translation.z = mat_remainder[3][2];
    translation *= getConfig().scale_factor;

    // Extract rotation
    rotation = extractQuat(mat_remainder);
}
