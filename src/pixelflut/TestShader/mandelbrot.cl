#define HEADER_SIZE 2
#define PIXELS_PER_BUFFER 160
#define BYTES_PER_PIXEL 7
#define BUFFER_SIZE (HEADER_SIZE + PIXELS_PER_BUFFER * BYTES_PER_PIXEL)


inline float3 hue_to_rgb(float hue)
{
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
    const float rainbow_scale,
    const float offset)
{
    const size_t pixel_index = get_global_id(0);
    const size_t pixel_count = (size_t)width * (size_t)height;
    if (pixel_index >= pixel_count)
    {
        return;
    }

    //
    // Find screen coordinate
    //
    const int x = (int)(pixel_index % width);
    const int y = (int)(pixel_index / width);


    //
    // Find sub-buffer and pixel offset inside it
    //
    const int buffer_number = pixel_index / PIXELS_PER_BUFFER;
    const int local_pixel = pixel_index % PIXELS_PER_BUFFER;
    const size_t buffer_offset = buffer_number * BUFFER_SIZE;

    
    //
    // First pixel in every sub-buffer writes the header
    //
    if (local_pixel == 0)
    {
        output[buffer_offset + 0] = 0x00; // Protocol 1
        output[buffer_offset + 1] = 0x01; // unused
    }


    //
    // Pixel data location
    //
    const size_t pixel_offset =
        buffer_offset +
        HEADER_SIZE +
        local_pixel * BYTES_PER_PIXEL;


    //
    // Write x and y as little endian int16
    //
    output[pixel_offset + 0] = (uchar)(x & 0xff);
    output[pixel_offset + 1] = (uchar)((x >> 8) & 0xff);
    output[pixel_offset + 2] = (uchar)(y & 0xff);
    output[pixel_offset + 3] = (uchar)((y >> 8) & 0xff);


    //
    // Map pixel to complex plane
    //
    float aspect = (float)width / (float)height;

    // Center on the interesting part of the set
    float cx = (((float)x / (float)(width - 1)) * 3.5f - 2.5f);
    float cy = (((float)y / (float)(height - 1)) * 2.0f - 1.0f);

    // Keep aspect ratio
    cx *= aspect;

    // Zoom and pan using your existing parameters
    float zoom = exp(offset * 0.15f);

    cx = (cx / zoom) - 0.743643887037151f;
       cy = (cy / zoom) + 0.131825904205330f;

    // Mandelbrot iteration
    float zx = 0.0f;
    float zy = 0.0f;

    const int max_iter = 256;
    int iter = 0;

    while (zx * zx + zy * zy <= 4.0f && iter < max_iter)
    {
        float xt = zx * zx - zy * zy + cx;
        zy = 2.0f * zx * zy + cy;
        zx = xt;
        iter++;
    }

    // Color
    float3 rgb;

    if (iter == max_iter)
    {
        rgb = (float3)(0.0f, 0.0f, 0.0f);
    }
    else
    {
        float mag = sqrt(zx * zx + zy * zy);
        float mu = iter + 1.0f - log2(log2(mag));

        float hue = mu * 0.02f;
        hue = hue - floor(hue);

        rgb = hue_to_rgb(hue);
    }

    //
    // RGB
    //
    output[pixel_offset + 4] = convert_uchar_sat_rte(rgb.x * 255.0f);
    output[pixel_offset + 5] = convert_uchar_sat_rte(rgb.y * 255.0f);
    output[pixel_offset + 6] = convert_uchar_sat_rte(rgb.z * 255.0f);
}