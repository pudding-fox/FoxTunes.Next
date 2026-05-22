using System;
using System.Windows.Media;

namespace FoxTunes
{
    public static class MoodBarColorProvider
    {
        public static Color GetColor(float[] bands)
        {
            // Expected:
            //
            // 0  = 60hz
            // 1  = 120hz
            // 2  = 250hz
            // 3  = 500hz
            // 4  = 1khz
            // 5  = 2khz
            // 6  = 4khz
            // 7  = 8khz
            // 8  = 12khz

            var b0 = bands[0];
            var b1 = bands[1];
            var b2 = bands[2];
            var b3 = bands[3];
            var b4 = bands[4];
            var b5 = bands[5];
            var b6 = bands[6];
            var b7 = bands[7];
            var b8 = bands[8];

            // --------------------------------------------------------------------
            // RGB spectral mapping.
            // --------------------------------------------------------------------

            float r =
                (b0 * 1.00f) +
                (b1 * 0.85f) +
                (b2 * 0.45f);

            float g =
                (b2 * 0.20f) +
                (b3 * 0.45f) +
                (b4 * 0.70f) +
                (b5 * 0.70f) +
                (b6 * 0.25f);

            float b =
                (b5 * 0.55f) +
                (b6 * 0.85f) +
                (b7 * 1.00f) +
                (b8 * 0.90f);

            // --------------------------------------------------------------------
            // Soft normalization.
            // --------------------------------------------------------------------

            var max =
                Math.Max(
                    r,
                    Math.Max(g, b)
                );

            if (max > 1f)
            {
                r /= max;
                g /= max;
                b /= max;
            }

            // --------------------------------------------------------------------
            // Mild gamma.
            // --------------------------------------------------------------------

            r = (float)Math.Pow(r, 0.8);
            g = (float)Math.Pow(g, 0.8);
            b = (float)Math.Pow(b, 0.8);

            // --------------------------------------------------------------------
            // Reduce red slightly.
            // --------------------------------------------------------------------

            r *= 0.92f;

            // --------------------------------------------------------------------
            // Add cool undertone.
            // --------------------------------------------------------------------

            b += 0.04f;

            // --------------------------------------------------------------------
            // Slight desaturation.
            // --------------------------------------------------------------------

            var grey =
                (r + g + b) / 3f;

            r = (r * 0.85f) + (grey * 0.15f);
            g = (g * 0.85f) + (grey * 0.15f);
            b = (b * 0.85f) + (grey * 0.15f);

            // --------------------------------------------------------------------
            // Gentle exposure boost.
            // --------------------------------------------------------------------

            const float exposure = 1.0f;

            r *= exposure;
            g *= exposure;
            b *= exposure;

            // --------------------------------------------------------------------
            // Clamp.
            // --------------------------------------------------------------------

            r = Clamp(r);
            g = Clamp(g);
            b = Clamp(b);

            return Color.FromRgb(
                (byte)(r * 255f),
                (byte)(g * 255f),
                (byte)(b * 255f)
            );
        }

        private static float Clamp(float value)
        {
            if (value < 0f)
            {
                return 0f;
            }

            if (value > 1f)
            {
                return 1f;
            }

            return value;
        }
    }
}