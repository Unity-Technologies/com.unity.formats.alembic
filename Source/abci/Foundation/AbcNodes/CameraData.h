#pragma once
struct CameraData {
    bool visibility = true;
    float focal_length = 0;
    abcV2 sensor_size = {0, 0};
    abcV2 lens_shift = {0, 0};
    abcV2 near_far = {0, 0};
};
