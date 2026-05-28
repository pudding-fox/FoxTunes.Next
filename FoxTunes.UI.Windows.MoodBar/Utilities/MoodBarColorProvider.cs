using System;
using System.Windows.Media;

namespace FoxTunes
{
    public class MoodBarColorProvider
    {
        public static Color GetColor(float[] bands, int tint)
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

            float r =
                (b0 * 1.65f) +
                (b1 * 1.20f) +
                (b2 * 0.20f);

            float g =
                (b2 * 0.08f) +
                (b3 * 0.85f) +
                (b4 * 1.35f) +
                (b5 * 0.75f);

            float b =
                (b5 * 0.25f) +
                (b6 * 1.10f) +
                (b7 * 1.65f) +
                (b8 * 1.55f);

            const float exposure = 1.9f;

            r *= exposure;
            g *= exposure;
            b *= exposure;

            r = 1f - (float)Math.Exp(-r);
            g = 1f - (float)Math.Exp(-g);
            b = 1f - (float)Math.Exp(-b);

            r = (float)Math.Pow(r, 0.75f);
            g = (float)Math.Pow(g, 0.75f);
            b = (float)Math.Pow(b, 0.75f);

            var maxChannel = Math.Max(r, Math.Max(g, b));
            var minChannel = Math.Min(r, Math.Min(g, b));
            var chroma = maxChannel - minChannel;

            const float chromaBoost = 1.45f;
            if (chroma > 0f)
            {
                r = minChannel + ((r - minChannel) * chromaBoost);
                g = minChannel + ((g - minChannel) * chromaBoost);
                b = minChannel + ((b - minChannel) * chromaBoost);
            }

            r = Clamp(r);
            g = Clamp(g);
            b = Clamp(b);

            float h, s, v;
            RgbToHsv(r, g, b, out h, out s, out v);

            s = (float)Math.Pow(s, 0.65f);
            s *= 1.75f;

            v = (float)Math.Pow(v, 0.82f);
            v *= 1.18f;

            s = Clamp(s);
            v = Clamp(v);

            h += tint * 0.9f;

            while (h >= 360f)
            {
                h -= 360f;
            }

            while (h < 0f)
            {
                h += 360f;
            }

            HsvToRgb(h, s, v, out r, out g, out b);

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