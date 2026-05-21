using FoxTunes.Interfaces;
using System;
using System.IO;

namespace FoxTunes
{
    public class MoodBarCache : StandardComponent
    {
        private static readonly string PREFIX = typeof(MoodBarCache).Name;

        public MoodBarGenerator.MoodBarGeneratorData Get(IOutputStream stream, int resolution)
        {
            var id = this.GetDataId(stream, resolution);
            var fileName = default(string);
            if (FileMetaDataStore.Exists(PREFIX, id, out fileName))
            {
                var data = default(MoodBarGenerator.MoodBarGeneratorData);
                if (this.TryLoad(fileName, out data))
                {
                    return data;
                }
            }
            return null;
        }

        public MoodBarGenerator.MoodBarGeneratorData GetOrCreate(IOutputStream stream, int resolution, Func<MoodBarGenerator.MoodBarGeneratorData> factory)
        {
            var id = this.GetDataId(stream, resolution);
            var fileName = default(string);
            if (FileMetaDataStore.Exists(PREFIX, id, out fileName))
            {
                var data = default(MoodBarGenerator.MoodBarGeneratorData);
                if (this.TryLoad(fileName, out data))
                {
                    return data;
                }
            }
            return factory();
        }

        protected virtual bool TryLoad(string fileName, out MoodBarGenerator.MoodBarGeneratorData data)
        {
            try
            {
                using (var stream = File.OpenRead(fileName))
                {
                    data = Serializer.Instance.Read(stream) as MoodBarGenerator.MoodBarGeneratorData;
                    return true;
                }
            }
            catch (Exception e)
            {
                Logger.Write(this, LogLevel.Warn, "Failed to load mood bar from file \"{0}\": {1}", fileName, e.Message);
            }
            data = null;
            return false;
        }

        public void Save(IOutputStream stream, MoodBarGenerator.MoodBarGeneratorData data)
        {
            var id = this.GetDataId(stream, data.Resolution);
            this.Save(id, data);
        }

        protected virtual void Save(string id, MoodBarGenerator.MoodBarGeneratorData data)
        {
            try
            {
                using (var stream = new MemoryStream())
                {
                    Serializer.Instance.Write(stream, data);
                    stream.Seek(0, SeekOrigin.Begin);
                    FileMetaDataStore.Write(PREFIX, id, stream);
                    stream.Seek(0, SeekOrigin.Begin);
                }
            }
            catch (Exception e)
            {
                Logger.Write(this, LogLevel.Warn, "Failed to save mood bar: {0}", e.Message);
            }
        }

        private string GetDataId(IOutputStream stream, int resolution)
        {
            var hashCode = default(int);
            unchecked
            {
                hashCode = (hashCode * 29) + stream.FileName.GetDeterministicHashCode();
                hashCode = (hashCode * 29) + stream.Length.GetHashCode();
                hashCode = (hashCode * 29) + resolution.GetHashCode();
            }
            return Math.Abs(hashCode).ToString();
        }

        public class Key : IEquatable<Key>
        {
            public Key(string fileName, long length, int resolution)
            {
                this.FileName = fileName;
                this.Length = length;
                this.Resolution = resolution;
            }

            public string FileName { get; private set; }

            public long Length { get; private set; }

            public int Resolution { get; private set; }

            public virtual bool Equals(Key other)
            {
                if (other == null)
                {
                    return false;
                }
                if (object.ReferenceEquals(this, other))
                {
                    return true;
                }
                if (!string.Equals(this.FileName, other.FileName, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
                if (this.Length != other.Length)
                {
                    return false;
                }
                if (this.Resolution != other.Resolution)
                {
                    return false;
                }
                return true;
            }

            public override bool Equals(object obj)
            {
                return this.Equals(obj as Key);
            }

            public override int GetHashCode()
            {
                var hashCode = default(int);
                unchecked
                {
                    if (!string.IsNullOrEmpty(this.FileName))
                    {
                        hashCode += this.FileName.ToLower().GetHashCode();
                    }
                    hashCode += this.Length.GetHashCode();
                    hashCode += this.Resolution.GetHashCode();
                }
                return hashCode;
            }

            public static bool operator ==(Key a, Key b)
            {
                if ((object)a == null && (object)b == null)
                {
                    return true;
                }
                if ((object)a == null || (object)b == null)
                {
                    return false;
                }
                if (object.ReferenceEquals((object)a, (object)b))
                {
                    return true;
                }
                return a.Equals(b);
            }

            public static bool operator !=(Key a, Key b)
            {
                return !(a == b);
            }
        }
    }
}
