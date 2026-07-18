inline float3 hue_to_rgb(float hue)
{
    // Convert hue in the range [0, 1) to an RGB rainbow.
    float3 phase = (float3)(0.0f, 4.0f, 2.0f);

    return clamp(
        fabs(fmod(hue * 6.0f + phase, 6.0f) - 3.0f) - 1.0f,
        0.0f,
        1.0f
    );
}

__kernel void process_buffer(
    __global uchar* output,
    const int width,
    const int height,
    const float multiplier,
    const float offset)
{
    const size_t index = get_global_id(0);
    const size_t pixel_count = (size_t)height * (size_t)width;

    if (index >= pixel_count)
        return;

    const int x = (int)(index % (size_t)width);
    const int y = (int)(index / (size_t)width);

    /*
     * x + y creates diagonal bands.
     *
     * offset shifts the rainbow diagonally.
     * multiplier controls the width of a full rainbow:
     *   multiplier = 1.0 -> width of approximately one screen width
     *   multiplier = 0.5 -> narrower rainbow
     *   multiplier = 2.0 -> wider rainbow
     */
    const float rainbow_width = fmax(1.0f, (float)width * fabs(multiplier));

    float hue = ((float)(x + y) - offset) / rainbow_width;

    // Wrap the hue so the rainbow repeats.
    hue = hue - floor(hue);

    const float3 rgb = hue_to_rgb(hue);
    const size_t byte_index = index * 3;
    output[byte_index + 0] = convert_uchar_sat_rte(rgb.x * 255.0f);
    output[byte_index + 1] = convert_uchar_sat_rte(rgb.y * 255.0f);
    output[byte_index + 2] = convert_uchar_sat_rte(rgb.z * 255.0f);
}