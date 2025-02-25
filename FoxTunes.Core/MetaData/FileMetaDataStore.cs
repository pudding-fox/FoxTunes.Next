using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FoxTunes
{
    public static class FileMetaDataStore
    {
        public static readonly string DataStoreDirectoryName = Path.Combine(
            Publication.RelativeStoragePath,
            "DataStore"
        );

        public static readonly KeyLock<string> KeyLock = new KeyLock<string>(StringComparer.OrdinalIgnoreCase);

        public static readonly object SyncRoot = new object();

        public static bool Contains(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return false;
            }
            return fileName.StartsWith(DataStoreDirectoryName, StringComparison.OrdinalIgnoreCase);
        }

        public static bool Exists(string prefix, string id, out string fileName)
        {
            fileName = GetFileName(prefix, id);
            return File.Exists(fileName);
        }

        public static string Write(string prefix, string id, byte[] data)
        {
            var fileName = GetFileName(prefix, id);
            LogManager.Logger.Write(typeof(FileMetaDataStore), LogLevel.Trace, "Writing data: {0} => {1}", id, fileName);
            Directory.CreateDirectory(Path.GetDirectoryName(fileName));
            try
            {
                using (var stream = File.Open(fileName, FileMode.Create))
                {
                    stream.Write(data, 0, data.Length);
                }
            }
            catch (Exception e)
            {
                LogManager.Logger.Write(typeof(FileMetaDataStore), LogLevel.Error, "Failed to write data: {0} => {1} => {2}", id, fileName, e.Message);
            }
            return fileName;
        }

        public static async Task<string> WriteAsync(string prefix, string id, byte[] data)
        {
            var fileName = GetFileName(prefix, id);
            LogManager.Logger.Write(typeof(FileMetaDataStore), LogLevel.Trace, "Writing data: {0} => {1}", id, fileName);
            Directory.CreateDirectory(Path.GetDirectoryName(fileName));
            try
            {
                using (var stream = File.Open(fileName, FileMode.Create))
                {
                    await stream.WriteAsync(data, 0, data.Length).ConfigureAwait(false);
                }
            }
            catch (Exception e)
            {
                LogManager.Logger.Write(typeof(FileMetaDataStore), LogLevel.Error, "Failed to write data: {0} => {1} => {2}", id, fileName, e.Message);
            }
            return fileName;
        }

        public static string Write(string prefix, string id, Stream stream)
        {
            var fileName = GetFileName(prefix, id);
            LogManager.Logger.Write(typeof(FileMetaDataStore), LogLevel.Trace, "Writing data: {0} => {1}", id, fileName);
            Directory.CreateDirectory(Path.GetDirectoryName(fileName));
            using (var file = File.OpenWrite(fileName))
            {
                try
                {
                    stream.CopyTo(file);
                }
                catch (Exception e)
                {
                    LogManager.Logger.Write(typeof(FileMetaDataStore), LogLevel.Error, "Failed to write data: {0} => {1} => {2}", id, fileName, e.Message);
                }
            }
            return fileName;
        }

        public static async Task<string> WriteAsync(string prefix, string id, Stream stream)
        {
            var fileName = GetFileName(prefix, id);
            LogManager.Logger.Write(typeof(FileMetaDataStore), LogLevel.Trace, "Writing data: {0} => {1}", id, fileName);
            Directory.CreateDirectory(Path.GetDirectoryName(fileName));
            using (var file = File.OpenWrite(fileName))
            {
                try
                {
                    await stream.CopyToAsync(file).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    LogManager.Logger.Write(typeof(FileMetaDataStore), LogLevel.Error, "Failed to write data: {0} => {1} => {2}", id, fileName, e.Message);
                }
            }
            return fileName;
        }

        public static string Copy(string prefix, string id, string fileName)
        {
            using (var stream = File.OpenRead(fileName))
            {
                return Write(prefix, id, stream);
            }
        }

        public static async Task<string> CopyAsync(string prefix, string id, string fileName)
        {
            using (var stream = File.OpenRead(fileName))
            {
                return await WriteAsync(prefix, id, stream).ConfigureAwait(false);
            }
        }

        public static void Clear(string prefix)
        {
            lock (SyncRoot)
            {
                var directoryName = Path.Combine(DataStoreDirectoryName, prefix);
                if (!Directory.Exists(directoryName))
                {
                    return;
                }
                try
                {
                    Directory.Delete(directoryName, true);
                }
                catch (Exception e)
                {
                    LogManager.Logger.Write(typeof(FileMetaDataStore), LogLevel.Error, "Failed to clear data: {0} => {1}", prefix, e.Message);
                }
            }
        }

        private static IEnumerable<string> GetSegments(string id)
        {
            return BitConverter.GetBytes(id.GetDeterministicHashCode()).Select(value => Convert.ToString(value));
        }

        public static string GetFileName(string prefix, string id)
        {
            return Path.Combine(DataStoreDirectoryName, prefix, string.Concat(Path.Combine(GetSegments(id).ToArray()), ".bin"));
        }

        public static string IfNotExists(string prefix, string id, Func<string, string> factory, bool always = false)
        {
            var result = default(string);
            if (!always && Exists(prefix, id, out result))
            {
                return result;
            }
            using (KeyLock.Lock(GetFileName(prefix, id)))
            {
                if (!always && Exists(prefix, id, out result))
                {
                    return result;
                }
                return factory(result);
            }
        }

        public static async Task<string> IfNotExistsAsync(string prefix, string id, Func<string, Task<string>> factory, bool always = false)
        {
            var result = default(string);
            if (!always && Exists(prefix, id, out result))
            {
                return result;
            }
            using (await KeyLock.LockAsync(GetFileName(prefix, id)).ConfigureAwait(false))
            {
                if (!always && Exists(prefix, id, out result))
                {
                    return result;
                }
                return await factory(result).ConfigureAwait(false);
            }
        }
    }
}
