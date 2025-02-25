using FoxTunes.Interfaces;
using System.Runtime.InteropServices;

namespace FoxTunes
{
    public static class WindowMessages
    {
        private static ILogger Logger
        {
            get
            {
                return LogManager.Logger;
            }
        }

        public static readonly int WM_TASKBARCREATED;

        static WindowMessages()
        {
            try
            {
                WM_TASKBARCREATED = RegisterWindowMessage("TaskbarCreated");
            }
            catch
            {
                Logger.Write(typeof(TaskbarButtonsBehaviour), LogLevel.Warn, "Failed to register window message: TaskbarCreated");
            }
        }

        [DllImport("user32.dll")]
        public static extern int RegisterWindowMessage(string msg);
    }
}
