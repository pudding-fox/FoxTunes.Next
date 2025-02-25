using System.Diagnostics;

namespace FoxTunes.Integration
{
    public static class Explorer
    {
        const string EXPLORER = "explorer.exe";

        public static void Select(string fileName)
        {
            var args = string.Format("/select, \"{0}\"", fileName);

            Process.Start(EXPLORER, args);
        }

        public static void Open(string fileName)
        {
            var args = string.Format("\"{0}\"", fileName);

            Process.Start(EXPLORER, args);
        }
    }
}
