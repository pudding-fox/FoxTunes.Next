using System;
using System.Windows.Media;

namespace FoxTunes
{
    public class MoodBarColorProvider
    {
        public static Color GetColor(float[] bands, int tint)
        {
            // Aggressive tweaks for bright neon output
            const float saturation = 1.6f;
            const float exposure = 1.35f;

            var b0 = bands[0];
            var b1 = bands[1];
            var b2 = bands[2];
            var b3 = bands[3];
            var b4 = bands[4];
            var b5 = bands[5];
            var b6 = bands[6];
            var b7 = bands[7];
            var b8 = bands[8];

            float r =
                (b0 * 1.25f) +
                (b1 * 1.00f) +
                (b2 * 0.35f);

            float g =
                (b2 * 0.15f) +
                (b3 * 0.60f) +
                (b4 * 1.00f) +
                (b5 * 0.85f);

            float b =
                (b5 * 0.45f) +
                (b6 * 0.90f) +
                (b7 * 1.25f) +
                (b8 * 1.15f);

            r *= exposure;
            g *= exposure;
            b *= exposure;

            r = r / (r + 1f);
            g = g / (g + 1f);
            b = b / (b + 1f);

            r = (float)Math.Pow(r, 0.9);
            g = (float)Math.Pow(g, 0.9);
            b = (float)Math.Pow(b, 0.9);

            var luma = (0.2126f * r) + (0.7152f * g) + (0.0722f * b);

            r = luma + ((r - luma) * saturation);
            g = luma + ((g - luma) * saturation);
            b = luma + ((b - luma) * saturation);

            r = Clamp(r);
            g = Clamp(g);
            b = Clamp(b);

            var h = default(float);
            var s = default(float);
            var v = default(float);
            RgbToHsv(r, g, b, out h, out s, out v);

            // Neon-specific boosts: push saturation and value towards 1
            s = Math.Min(1f, s * 1.5f);
            v = Math.Min(1f, v * 1.12f);

            h += tint * 0.6f;
            while (h >= 360f) h -= 360f;
            while (h < 0f) h += 360f;

            HsvToRgb(h, s, v, out r, out g, out b);

            // Ensure final RGB are in [0,1]
            r = Clamp(r);
            g = Clamp(g);
            b = Clamp(b);

            return Color.FromRgb(
                (byte)(r * 255f),
                (byte)(g * 255f),
                (byte)(b * 255f)
            );
        }

        private static void RgbToHsv(float r, float g, float b, out float h, out float s, out float v)
        {
            var max = Math.Max(r, Math.Max(g, b));
            var min = Math.Min(r, Math.Min(g, b));
            var delta = max - min;
            h = 0f;
            if (delta > 0f)
            {
                if (max == r)
                {
                    h = 60f * (((g - b) / delta) % 6f);
                }
                else if (max == g)
                {
                    h = 60f * (((b - r) / delta) + 2f);
                }
                else
                {
                    h = 60f * (((r - g) / delta) + 4f);
                }
            }
            if (h < 0f)
            {
                h += 360f;
            }
            s = max <= 0f ? 0f : delta / max;
            v = max;
        }

        private static void HsvToRgb(float h, float s, float v, out float r, out float g, out float b)
        {
            var c = v * s;
            var x = c * (1f - Math.Abs(((h / 60f) % 2f) - 1f));
            var m = v - c;
            var rr = default(float);
            var gg = default(float);
            var bb = default(float);
            if (h < 60f)
            {
                rr = c;
                gg = x;
                bb = 0f;
            }
            else if (h < 120f)
            {
                rr = x;
                gg = c;
                bb = 0f;
            }
            else if (h < 180f)
            {
                rr = 0f;
                gg = c;
                bb = x;
            }
            else if (h < 240f)
            {
                rr = 0f;
                gg = x;
                bb = c;
            }
            else if (h < 300f)
            {
                rr = x;
                gg = 0f;
                bb = c;
            }
            else
            {
                rr = c;
                gg = 0f;
                bb = x;
            }
            r = rr + m;
            g = gg + m;
            b = bb + m;
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