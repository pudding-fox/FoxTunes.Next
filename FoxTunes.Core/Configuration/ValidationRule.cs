namespace FoxTunes
{
    public abstract class ValidationRule
    {
        public abstract bool Validate(object value, out string message);
    }
}
