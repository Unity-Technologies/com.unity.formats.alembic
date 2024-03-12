#pragma once

#include <cstdint>
#include "pch.h"
#include <../Foundation/AbcNodes/CameraData.h>

abcSampleSelector aiTimeToSampleSelector(double time);
abcSampleSelector aiIndexToSampleSelector(int64_t index);
