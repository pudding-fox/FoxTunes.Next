using System;
using System.IO;
using System.Text.RegularExpressions;

namespace FoxTunes
{
    public static class PresetComplexity
    {
        public static string Analyze(string fileName)
        {
            var preset = File.ReadAllText(fileName);

            var score = 0f;

            // 1. Shader detection (VERY heavy)
            if (preset.Contains("warp_1=`shader_body")) score += 25;
            if (preset.Contains("comp_1=`shader_body")) score += 25;

            // 2. Count per_point lines (heavy CPU)
            var perPointLines = Regex.Matches(preset, @"per_point\d*=").Count;
            score += perPointLines * 0.15f;

            // 3. Count per_frame lines (moderate CPU)
            var perFrameLines = Regex.Matches(preset, @"per_frame\d*=").Count;
            score += perFrameLines * 0.05f;

            // 4. Shape instancing
            var shapeInstMatches = Regex.Matches(preset, @"shapecode_\d+_num_inst=(\d+)");
            foreach (Match m in shapeInstMatches)
            {
                if (int.TryParse(m.Groups[1].Value, out var count))
                {
                    score += count * 0.2f;
                }
            }

            // 5. Motion vectors
            var mvX = ExtractInt(preset, "nMotionVectorsX");
            var mvY = ExtractInt(preset, "nMotionVectorsY");
            score += (mvX * mvY) * 0.002f;

            // 6. Math-heavy operations
            var heavyOps = new string[] { "sin(", "cos(", "pow(", "sqrt(", "tan(" };
            foreach (var op in heavyOps)
            {
                var count = Regex.Matches(preset, Regex.Escape(op)).Count;
                score += count * 0.1f;
            }

            // 7. Shader texture sampling (VERY expensive)
            var texOps = new string[] { "tex2D", "tex3D", "GetBlur", "GetPixel" };
            foreach (var op in texOps)
            {
                var count = Regex.Matches(preset, op).Count;
                score += count * 0.5f;
            }

            // Normalize score
            var finalScore = (int)Math.Min(100, score);

            return Classify(finalScore);
        }

        private static int ExtractInt(string text, string key)
        {
            var match = Regex.Match(text, key + @"=([\d\.]+)");
            if (match.Success && double.TryParse(match.Groups[1].Value, out double val))
                return (int)val;
            return 0;
        }

        private static string Classify(int score)
        {
            if (score < 20) return "Low";
            if (score < 40) return "Medium";
            if (score < 70) return "High";
            return "Extreme";
        }
    }
}
