namespace FoxTunes
{
    public class DoubleValidationRule : ValidationRule
    {
        public DoubleValidationRule(double minValue, double maxValue, double step = 1)
        {
            this.MinValue = minValue;
            this.MaxValue = maxValue;
            this.Step = step;
        }

        public double MinValue { get; private set; }

        public double MaxValue { get; private set; }

        public double Step { get; private set; }

        public override bool Validate(object value, out string message)
        {
            var numeric = default(double);
            if (value is double)
            {
                numeric = (double)value;
            }
            else if (value is string)
            {
                if (!double.TryParse((string)value, out numeric))
                {
                    message = "Numeric value expected.";
                    return false;
                }
            }
            return this.Validate(numeric, out message);
        }

        protected virtual bool Validate(double value, out string message)
        {
            if (value < this.MinValue)
            {
                message = string.Format("Numeric value greater than or equal to {0} expected.", this.MinValue);
                return false;
            }
            if (value > this.MaxValue)
            {
                message = string.Format("Numeric value less than or equal to {0} expected.", this.MaxValue);
                return false;
            }
            message = null;
            return true;
        }
    }
}
