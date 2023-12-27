using ManagedBass;
using ManagedBass.Asio;
using System;

namespace FoxTunes
{
    public static class BassAsioUtils
    {
        public static bool OK(bool result)
        {
            if (!result)
            {
                if (Bass.LastError != Errors.OK || BassAsio.LastError != Errors.OK)
                {
                    Throw();
                }
            }
            return result;
        }

        public static int OK(int result)
        {
            if (result == 0)
            {
                if (Bass.LastError != Errors.OK || BassAsio.LastError != Errors.OK)
                {
                    Throw();
                }
            }
            return result;
        }

        public static void Throw()
        {
            if (Bass.LastError != Errors.OK)
            {
                throw new BassException(Bass.LastError);
            }
            if (BassAsio.LastError != Errors.OK)
            {
                throw new BassException(BassAsio.LastError);
            }
        }
    }
}
