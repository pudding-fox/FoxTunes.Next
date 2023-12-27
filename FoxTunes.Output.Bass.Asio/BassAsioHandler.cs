using System;
using System.Runtime.InteropServices;

namespace FoxTunes
{
    public static class BassAsioHandler
    {
        const string DllName = "bass_asio_handler";

        [DllImport(DllName)]
        static extern bool BASS_ASIO_HANDLER_Init();

        /// <summary>
        /// Initialize.
        /// </summary>
        /// <returns></returns>
        public static bool Init()
        {
            return BASS_ASIO_HANDLER_Init();
        }

        [DllImport(DllName)]
        static extern bool BASS_ASIO_HANDLER_Free();

        /// <summary>
        /// Free.
        /// </summary>
        /// <returns></returns>
        public static bool Free()
        {
            return BASS_ASIO_HANDLER_Free();
        }

        [DllImport(DllName)]
        static extern bool BASS_ASIO_HANDLER_StreamGet(out int Handle);

        public static bool StreamGet(out int Handle)
        {
            return BASS_ASIO_HANDLER_StreamGet(out Handle);
        }

        [DllImport(DllName)]
        static extern bool BASS_ASIO_HANDLER_StreamSet(int Handle);

        public static bool StreamSet(int Handle)
        {
            return BASS_ASIO_HANDLER_StreamSet(Handle);
        }

        [DllImport(DllName)]
        static extern bool BASS_ASIO_HANDLER_ChannelEnable(bool Input, int Channel, IntPtr User = default(IntPtr));

        public static bool ChannelEnable(bool Input, int Channel, IntPtr User = default(IntPtr))
        {
            return BASS_ASIO_HANDLER_ChannelEnable(Input, Channel, User);
        }
    }
}