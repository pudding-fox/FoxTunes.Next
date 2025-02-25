using ManagedBass.Wasapi;
using System;
using System.Runtime.InteropServices;

namespace FoxTunes
{
    public static class BassWasapiHandler
    {
        const string DllName = "bass_wasapi_handler";

        [DllImport(DllName)]
        static extern bool BASS_WASAPI_HANDLER_Init(int Device, int Frequency, int Channels, WasapiInitFlags Flags, float Buffer, float Period, IntPtr User);

        /// <summary>
        /// Initialize.
        /// </summary>
        /// <returns></returns>
        public static bool Init(int Device, int Frequency = 0, int Channels = 0, WasapiInitFlags Flags = WasapiInitFlags.Shared, float Buffer = 0f, float Period = 0f, IntPtr User = default(IntPtr))
        {
            return BASS_WASAPI_HANDLER_Init(Device, Frequency, Channels, Flags, Buffer, Period, User);
        }

        [DllImport(DllName)]
        static extern bool BASS_WASAPI_HANDLER_Free();

        /// <summary>
        /// Free.
        /// </summary>
        /// <returns></returns>
        public static bool Free()
        {
            return BASS_WASAPI_HANDLER_Free();
        }

        [DllImport(DllName)]
        static extern bool BASS_WASAPI_HANDLER_StreamGet(out int Handle);

        public static bool StreamGet(out int Handle)
        {
            return BASS_WASAPI_HANDLER_StreamGet(out Handle);
        }

        [DllImport(DllName)]
        static extern bool BASS_WASAPI_HANDLER_StreamSet(int Handle);

        public static bool StreamSet(int Handle)
        {
            return BASS_WASAPI_HANDLER_StreamSet(Handle);
        }
    }
}