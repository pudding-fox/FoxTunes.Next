using System;

namespace FoxTunes
{
    public static class FormatterFactory
    {
        public static FormatterBase Create(string format)
        {
            if (TimeSpanFormatter.CanFormat(format))
            {
                return new TimeSpanFormatter(format);
            }
            if (TimeStampFormatter.CanFormat(format))
            {
                return new TimeStampFormatter(format);
            }
            if (DecibelFormatter.CanFormat(format))
            {
                return new DecibelFormatter(format);
            }
            if (FloatFormatter.CanFormat(format))
            {
                return new FloatFormatter(format);
            }
            if (IntegerFormatter.CanFormat(format))
            {
                return new IntegerFormatter(format);
            }
            if (SizeFormatter.CanFormat(format))
            {
                return new SizeFormatter(format);
            }
            return new StringFormatter(format);
        }

        public abstract class FormatterBase
        {
            protected FormatterBase(string format)
            {
                this.Format = format;
            }

            public string Format { get; private set; }

            public abstract object GetValue(object value);
        }

        public class StringFormatter : FormatterBase
        {
            public StringFormatter(string format) : base(format)
            {

            }

            public override object GetValue(object value)
            {
                if (string.IsNullOrEmpty(this.Format))
                {
                    return Convert.ToString(value);
                }
                return string.Format(this.Format, value);
            }
        }

        public class TimeSpanFormatter : FormatterBase
        {
            public TimeSpanFormatter(string format) : base(format)
            {

            }

            public override object GetValue(object value)
            {
                var ms = default(int);
                if (!int.TryParse(Convert.ToString(value), out ms))
                {
                    return value;
                }

                var s = Convert.ToInt32((ms / 1000) % 60);
                var m = Convert.ToInt32((ms / (1000 * 60)) % 60);
                var h = Convert.ToInt32((ms / (1000 * 60 * 60)) % 24);

                if (h > 0)
                {
                    return string.Format("{0:00}:{1:00}:{2:00}", h, m, s);
                }

                return string.Format("{0:00}:{1:00}", m, s);
            }

            public static bool CanFormat(string format)
            {
                return string.Equals(format, CommonFormats.TimeSpan, StringComparison.OrdinalIgnoreCase);
            }
        }

        public class TimeStampFormatter : FormatterBase
        {
            public TimeStampFormatter(string format) : base(format)
            {

            }

            public override object GetValue(object value)
            {
                var date = DateTimeHelper.FromString(Convert.ToString(value));
                if (date == default(DateTime))
                {
                    return value;
                }
                return string.Concat(date.ToShortDateString(), " ", date.ToShortTimeString());
            }

            public static bool CanFormat(string format)
            {
                return string.Equals(format, CommonFormats.TimeStamp, StringComparison.OrdinalIgnoreCase);
            }
        }

        public class DecibelFormatter : FormatterBase
        {
            public DecibelFormatter(string format) : base(format)
            {

            }

            public override object GetValue(object value)
            {
                var parsed = default(float);
                if (!float.TryParse(Convert.ToString(value), out parsed))
                {
                    return value;
                }

                if (float.IsNaN(parsed))
                {
                    return value;
                }

                return string.Format("{0}{1:0.00}dB", parsed > 0 ? "+" : string.Empty, parsed);
            }

            public static bool CanFormat(string format)
            {
                return string.Equals(format, CommonFormats.Decibel, StringComparison.OrdinalIgnoreCase);
            }
        }

        public class FloatFormatter : FormatterBase
        {
            public FloatFormatter(string format) : base(format)
            {

            }

            public override object GetValue(object value)
            {
                var parsed = default(float);
                if (!float.TryParse(Convert.ToString(value), out parsed))
                {
                    return value;
                }

                if (float.IsNaN(parsed))
                {
                    return value;
                }

                return parsed.ToString("0.000000");
            }

            public static bool CanFormat(string format)
            {
                return string.Equals(format, CommonFormats.Float, StringComparison.OrdinalIgnoreCase);
            }
        }

        public class IntegerFormatter : FormatterBase
        {
            public IntegerFormatter(string format) : base(format)
            {

            }

            public override object GetValue(object value)
            {
                var parsed = default(int);
                if (!int.TryParse(Convert.ToString(value), out parsed))
                {
                    return value;
                }

                return parsed;
            }

            public static bool CanFormat(string format)
            {
                return string.Equals(format, CommonFormats.Integer, StringComparison.OrdinalIgnoreCase);
            }
        }

        public class SizeFormatter : FormatterBase
        {
            private static readonly string[] SUFFIX = { "B", "KB", "MB", "GB", "TB" };

            public SizeFormatter(string format) : base(format)
            {

            }

            public override object GetValue(object value)
            {
                var total = default(int);
                if (!int.TryParse(Convert.ToString(value), out total))
                {
                    return value;
                }
                var length = total;
                var order = 0;
                while (length >= 1024 && order < SUFFIX.Length - 1)
                {
                    order++;
                    length = length / 1024;
                }
                return string.Format("{0:0.##} {1} ({2} bytes)", length, SUFFIX[order], total);
            }

            public static bool CanFormat(string format)
            {
                return string.Equals(format, CommonFormats.Size, StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}
