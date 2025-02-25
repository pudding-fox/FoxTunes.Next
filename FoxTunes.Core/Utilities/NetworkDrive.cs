using FoxDb;
using FoxTunes.Interfaces;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace FoxTunes
{
    //TODO: This code is platform specific and should be in the FoxTunes.Core.Windows package.
    public static class NetworkDrive
    {
        private static ILogger Logger
        {
            get
            {
                return LogManager.Logger;
            }
        }

        public static bool IsRemotePath(string path)
        {
            var letter = Path.GetPathRoot(path);
            if (string.IsNullOrEmpty(letter) || letter.Length > 3)
            {
                return false;
            }
            var info = new DriveInfo(letter);
            var isRemotePath = info.DriveType == DriveType.Network || info.DriveType == DriveType.NoRootDirectory;
            if (isRemotePath)
            {
                Logger.Write(typeof(NetworkDrive), LogLevel.Debug, "{0} is a remote path.", path);
                return true;
            }
            else
            {
                Logger.Write(typeof(NetworkDrive), LogLevel.Debug, "{0} is not a remote path.", path);
                return false;
            }
        }

        /// <summary>
        /// There are many obscure native functions for reconnecting a network drive.
        /// They're all extremely complicated, requiring the UNC path, credentials (or a window handle to prompt for them).
        /// This routine simple opens explorer.exe on the drive letter then closes any resulting windows.
        /// The process created exits immidiately; a different process belongs to the window.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static async Task<bool> ConnectRemotePath(string path)
        {
            Logger.Write(typeof(NetworkDrive), LogLevel.Debug, "Attempting to connect remote path: {0}", path);

            var letter = Path.GetPathRoot(path);
            var info = new DriveInfo(letter);

            if (info.IsReady)
            {
                Logger.Write(typeof(NetworkDrive), LogLevel.Debug, "Drive is ready: {0}.", letter);
                return true;
            }

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer",
                    Arguments = letter,
                    WindowStyle = ProcessWindowStyle.Minimized
                });
            }
            catch (Exception e)
            {
                Logger.Write(typeof(NetworkDrive), LogLevel.Warn, "Failed to start explorer: {0}", e.Message);
                return false;
            }

            for (var a = 0; a < 10; a++)
            {
                if (info.IsReady)
                {
                    break;
                }
#if NET40
                await TaskEx.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
#else
                await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
#endif
            }
            try
            {
                foreach (var process in Process.GetProcessesByName("explorer"))
                {
                    if (!string.IsNullOrEmpty(process.MainWindowTitle) && process.MainWindowTitle.EndsWith(string.Format("({0}:)", letter[0])))
                    {
                        process.Kill();
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Write(typeof(NetworkDrive), LogLevel.Warn, "Failed to stop explorer: {0}", e.Message);
            }

            if (info.IsReady)
            {
                Logger.Write(typeof(NetworkDrive), LogLevel.Debug, "Drive is ready: {0}.", letter);
                return true;
            }
            else
            {
                Logger.Write(typeof(NetworkDrive), LogLevel.Debug, "Drive is not ready: {0}.", letter);
                return false;
            }
        }
    }
}
