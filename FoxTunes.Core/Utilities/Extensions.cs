using System;
using System.Linq;

namespace FoxTunes
{
    public static partial class Extensions
    {
        public static Exception Unwrap(this AggregateException exception)
        {
            var exceptions = exception.InnerExceptions.ToArray();
            switch (exceptions.Length)
            {
                case 0:
                    //This shouldn't happen.
                    return exception;
                case 1:
                    return exceptions[0];
                default:
                    return exception;
            }
        }
    }
}
