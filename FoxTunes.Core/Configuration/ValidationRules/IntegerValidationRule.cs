namespace FoxTunes
{
    public class IntegerValidationRule : ValidationRule
    {
        public IntegerValidationRule(int minValue, int maxValue, int step = 1)
        {
            this.MinValue = minValue;
            this.MaxValue = maxValue;
            this.Step = step;
        }

        public int MinValue { get; private set; }

        public int MaxValue { get; private set; }

        public int Step { get; private set; }

        public override bool Validate(object value, out string message)
        {
            var numeric = default(int);
            if (value is int)
            {
                numeric = (int)value;
            }
            else if (value is string)
            {
                if (!int.TryParse((string)value, out numeric))
                {
                    message = "Numeric value expected.";
                    return false;
                }
            }
            return this.Validate(numeric, out message);
        }

        protected virtual bool Validate(int value, out string message)
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
