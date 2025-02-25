using FoxTunes.Interfaces;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace FoxTunes
{
    [ComponentDependency(Slot = ComponentSlots.UserInterface)]
    public class FileAssociations : StandardComponent, IFileAssociations
    {
        public string Id
        {
            get
            {
                return Process.GetCurrentProcess().MainModule.ModuleName;
            }
        }

        public string FileName
        {
            get
            {
                return Process.GetCurrentProcess().MainModule.FileName;
            }
        }

        public IEnumerable<IFileAssociation> Associations
        {
            get
            {
                foreach (var extension in GetAssociations(this.Create(null)))
                {
                    yield return this.Create(extension);
                }
            }
        }

        public IFileAssociation Create(string extension)
        {
            if (!string.IsNullOrEmpty(extension))
            {
                if (!extension.StartsWith("."))
                {
                    extension = "." + extension;
                }
            }
            return new FileAssociation(extension, this.Id, this.FileName);
        }

        public bool IsAssociated(string extension)
        {
            return this.Associations.Contains(this.Create(extension));
        }

        public void Enable()
        {
            AddShortcuts(this.Create(null));
            AddProgram(this.Create(null));
        }

        public void Disable()
        {
            RemoveShortcuts(this.Create(null));
            RemoveProgram(this.Create(null));
        }

        public void Enable(IEnumerable<IFileAssociation> associations)
        {
            AddAssociations(associations);
        }

        public void Disable(IEnumerable<IFileAssociation> associations)
        {
            RemoveAssociations(associations);
        }

        [DllImport("Shell32.dll")]
        private static extern int SHChangeNotify(int eventId, int flags, IntPtr item1, IntPtr item2);

        private const int SHCNE_ASSOCCHANGED = 0x8000000;
        private const int SHCNF_FLUSH = 0x1000;

        private static IEnumerable<string> GetAssociations(IFileAssociation association)
        {
            foreach (var key in GetKeys(@"Software\Classes", association.ProgId))
            {
                yield return key;
            }
        }

        private static string GetOpenString(string path)
        {
            return string.Format("\"{0}\" \"%1\"", path);
        }

        private static void AddAssociations(IEnumerable<IFileAssociation> associations)
        {
            var success = false;
            foreach (var association in associations)
            {
                success |= AddAssociation(association);
            }
            if (success)
            {
                Logger.Write(typeof(FileAssociations), LogLevel.Debug, "Sending SHChangeNotify.");
                SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_FLUSH, IntPtr.Zero, IntPtr.Zero);
            }
        }

        private static void RemoveAssociations(IEnumerable<IFileAssociation> associations)
        {
            var success = false;
            foreach (var association in associations)
            {
                success |= RemoveAssociation(association);
            }
            if (success)
            {
                Logger.Write(typeof(FileAssociations), LogLevel.Debug, "Sending SHChangeNotify.");
                SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_FLUSH, IntPtr.Zero, IntPtr.Zero);
            }
        }

        private static void AddShortcuts(IFileAssociation association)
        {
            var fileName = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.SendTo),
                string.Format("{0}.lnk", association.ProgId)
            );
            if (global::System.IO.File.Exists(fileName))
            {
                global::System.IO.File.Delete(fileName);
            }
            Logger.Write(typeof(FileAssociations), LogLevel.Debug, "Creating \"SendTo\" link for program \"{0}\": {1}", association.ProgId, fileName);
            Shell.CreateShortcut(fileName, association.ExecutableFilePath);
        }

        private static void RemoveShortcuts(IFileAssociation association)
        {
            var fileName = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.SendTo),
                string.Format("{0}.lnk", association.ProgId)
            );
            if (!global::System.IO.File.Exists(fileName))
            {
                return;
            }
            Logger.Write(typeof(FileAssociations), LogLevel.Debug, "Removing \"SendTo\" link for program \"{0}\".", association.ProgId);
            global::System.IO.File.Delete(fileName);
        }

        private static bool AddProgram(IFileAssociation association)
        {
            var path = string.Format(@"Software\Classes\{0}\shell\open\command", association.ProgId);
            var command = GetOpenString(association.ExecutableFilePath);
            Logger.Write(typeof(FileAssociations), LogLevel.Debug, "Creating shell command for program \"{0}\": {1}", association.ProgId, command);
            return SetKeyValue(path, command);
        }

        private static bool RemoveProgram(IFileAssociation association)
        {
            var path = @"Software\Classes\" + association.ProgId;
            Logger.Write(typeof(FileAssociations), LogLevel.Debug, "Removing shell command for program \"{0}\".", association.ProgId);
            return DeleteKey(path);
        }

        private static bool AddAssociation(IFileAssociation association)
        {
            var path = @"Software\Classes\" + association.Extension;
            Logger.Write(typeof(FileAssociations), LogLevel.Debug, "Creating file association for program \"{0}\": {1}", association.ProgId, association.Extension);
            return SetKeyValue(path, association.ProgId);
        }

        private static bool RemoveAssociation(IFileAssociation association)
        {
            var path = @"Software\Classes\" + association.Extension;
            Logger.Write(typeof(FileAssociations), LogLevel.Debug, "Removing file association for program \"{0}\": {1}", association.ProgId, association.Extension);
            return DeleteKey(path);
        }

        private static IEnumerable<string> GetKeys(string path, string value)
        {
            using (var key = Registry.CurrentUser.OpenSubKey(path))
            {
                foreach (var name in key.GetSubKeyNames())
                {
                    if (string.Equals(GetKeyValue(string.Format(@"{0}\{1}", path, name)), value, StringComparison.OrdinalIgnoreCase))
                    {
                        yield return name;
                    }
                }
            }
        }

        private static string GetKeyValue(string path)
        {
            using (var key = Registry.CurrentUser.OpenSubKey(path))
            {
                if (key == null)
                {
                    return null;
                }
                return key.GetValue(null) as string;
            }
        }

        private static bool SetKeyValue(string path, string value)
        {
            using (var key = Registry.CurrentUser.CreateSubKey(path))
            {
                if (string.Equals(key.GetValue(null) as string, value, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
                if (string.Equals(Convert.ToString(key.GetValue(null)), value, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
                key.SetValue(null, value);
                return true;
            }
        }

        private static bool DeleteKey(string path)
        {
            using (var key = Registry.CurrentUser.OpenSubKey(path))
            {
                if (key == null)
                {
                    return false;
                }
            }
            Registry.CurrentUser.DeleteSubKeyTree(path);
            return true;
        }
    }
}
