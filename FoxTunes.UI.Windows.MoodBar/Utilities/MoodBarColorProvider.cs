using System;
using System.Windows.Media;

namespace FoxTunes
{
    public static class MoodBarColorProvider
    {
        public static Color GetColor(float[] bands)
        {
            var b0 = bands[0];
            var b1 = bands[1];
            var b2 = bands[2];
            var b3 = bands[3];
            var b4 = bands[4];
            var b5 = bands[5];
            var b6 = bands[6];
            var b7 = bands[7];
            var b8 = bands[8];

            // Simpler spectral mapping.

            float r =
                (b0 * 1.00f) +
                (b1 * 0.85f) +
                (b2 * 0.45f);

            float g =
                (b2 * 0.35f) +
                (b3 * 0.80f) +
                (b4 * 0.60f);

            float b =
                (b5 * 0.55f) +
                (b6 * 0.85f) +
                (b7 * 1.00f) +
                (b8 * 0.90f);

            // Soft normalize only if needed.

            var max =
                Math.Max(r, Math.Max(g, b));

            if (max > 1f)
            {
                r /= max;
                g /= max;
                b /= max;
            }

            // Mild gamma only.

            r = (float)Math.Pow(r, 0.8);
            g = (float)Math.Pow(g, 0.8);
            b = (float)Math.Pow(b, 0.8);

            // Small saturation reduction.

            var grey =
                (r + g + b) / 3f;

            r = (r * 0.85f) + (grey * 0.15f);
            g = (g * 0.85f) + (grey * 0.15f);
            b = (b * 0.85f) + (grey * 0.15f);

            // Gentle brightness boost.

            const float exposure = 1.25f;

            r *= exposure;
            g *= exposure;
            b *= exposure;

            // Clamp.

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