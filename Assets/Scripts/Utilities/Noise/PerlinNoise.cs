using Unity.Mathematics;

/// <summary>
/// Burst-compatible Perlin noise implementation.
/// Based on improved Perlin noise by Ken Perlin (2002).
/// </summary>
public static class PerlinNoise
{
    // Permutation table for noise generation (FIXED - all values 0-255)
    private static readonly int[] Permutation = new int[256]
    {
        151,
        160,
        137,
        91,
        90,
        15,
        131,
        13,
        201,
        95,
        96,
        53,
        194,
        233,
        7,
        225,
        140,
        36,
        103,
        30,
        69,
        142,
        8,
        99,
        37,
        240,
        21,
        10,
        23,
        190,
        6,
        148,
        247,
        120,
        234,
        75,
        0,
        26,
        197,
        62,
        94,
        252,
        219,
        203,
        117,
        35,
        11,
        32,
        57,
        177,
        33,
        88,
        237,
        149,
        56,
        87,
        174,
        20,
        125,
        136,
        171,
        168,
        68,
        175,
        74,
        165,
        71,
        134,
        139,
        48,
        27,
        166,
        77,
        146,
        158,
        231,
        83,
        109,
        122,
        178,
        210,
        180,
        115,
        103,
        212,
        149,
        3,
        246,
        97,
        53,
        87,
        185,
        134,
        193,
        29,
        158,
        225,
        248,
        152,
        17,
        105,
        217,
        142,
        148,
        155,
        30,
        135,
        233,
        206,
        85,
        40,
        223,
        140,
        161,
        137,
        13,
        191,
        230,
        33,
        98,
        52,
        51,
        201,
        74,
        128,
        227,
        32,
        72,
        15,
        16,
        97,
        75,
        32,
        71,
        96,
        214,
        40,
        91,
        85,
        254,
        2,
        41,
        77,
        236,
        187,
        51,
        72,
        60,
        43,
        37,
        151,
        32,
        251,
        54,
        5,
        52,
        133,
        63,
        13,
        82,
        107,
        53,
        87,
        185,
        134,
        193,
        29,
        158,
        225,
        248,
        152,
        17,
        105,
        217,
        142,
        148,
        155,
        30,
        135,
        233,
        206,
        85,
        40,
        223,
        140,
        161,
        137,
        13,
        191,
        230,
        33,
        98,
        52,
        51,
        201,
        74,
        128,
        227,
        32,
        72,
        15,
        16,
        97,
        75,
        32,
        71,
        96,
        214,
        40,
        91,
        85,
        254,
        2,
        41,
        77,
        236,
        187,
        51,
        72,
        60,
        43,
        37,
        151,
        32,
        251,
        54,
        5,
        52,
        133,
        63,
        13,
        82,
        107,
        53,
        87,
        185,
        134,
        193,
        29,
        158,
        225,
        248,
        152,
        17,
        105,
        217,
        142,
        148,
        155,
        30,
        135,
        233,
        206,
        85,
        40,
        223,
    };

    /// <summary>
    /// 2D Perlin noise. Returns value in range [0, 1].
    /// </summary>
    public static float Noise(float2 position)
    {
        // Get integer grid coordinates
        int xi = (int)math.floor(position.x) & 255;
        int yi = (int)math.floor(position.y) & 255;

        // Get fractional offsets within grid cell
        float xf = position.x - math.floor(position.x);
        float yf = position.y - math.floor(position.y);

        // Fade curves (smootherstep for smooth interpolation)
        float u = Fade(xf);
        float v = Fade(yf);

        // Get gradient indices from permutation table
        int g00 = Permutation[(Permutation[xi] + yi) & 255];
        int g10 = Permutation[(Permutation[(xi + 1) & 255] + yi) & 255];
        int g01 = Permutation[(Permutation[xi] + (yi + 1) & 255) & 255];
        int g11 = Permutation[(Permutation[(xi + 1) & 255] + (yi + 1) & 255) & 255];

        // Compute dot products with gradients
        float d00 = Gradient(g00, xf, yf);
        float d10 = Gradient(g10, xf - 1, yf);
        float d01 = Gradient(g01, xf, yf - 1);
        float d11 = Gradient(g11, xf - 1, yf - 1);

        // Interpolate
        float x0 = math.lerp(d00, d10, u);
        float x1 = math.lerp(d01, d11, u);
        float result = math.lerp(x0, x1, v);

        // Remap from [-1, 1] to [0, 1]
        return result * 0.5f + 0.5f;
    }

    // Smootherstep fade function (smoother than smoothstep)
    private static float Fade(float t)
    {
        return t * t * t * (t * (t * 6 - 15) + 10);
    }

    // Gradient dot product (simplified for 2D)
    private static float Gradient(int hash, float x, float y)
    {
        int h = hash & 15;
        float u = h < 8 ? x : y;
        float v = h < 8 ? y : x;
        return ((h & 1) == 0 ? u : -u) + ((h & 2) == 0 ? v : -v);
    }
}
