using System;

namespace FoxTunes
{
    public class OutputException : Exception
    {
        public OutputException(string message)
            : base(message)
        {

        }
    }
}
