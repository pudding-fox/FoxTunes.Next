using FoxTunes.Interfaces;
using System;

namespace FoxTunes
{
    public class FileAssociation : IFileAssociation
    {
        public FileAssociation(string extension, string progId, string executableFilePath)
        {
            this.Extension = extension;
            this.ProgId = progId;
            this.ExecutableFilePath = executableFilePath;
        }

        public string Extension { get; private set; }

        public string ProgId { get; private set; }

        public string ExecutableFilePath { get; private set; }

        public bool Equals(FileAssociation other)
        {
            if (other == null)
            {
                return false;
            }
            if (object.ReferenceEquals(this, other))
            {
                return true;
            }
            if (!string.Equals(this.Extension, other.Extension, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            if (!string.Equals(this.ProgId, other.ProgId, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            if (!string.Equals(this.ExecutableFilePath, other.ExecutableFilePath, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            return true;
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as FileAssociation);
        }

        public override int GetHashCode()
        {
            var hashCode = 0;
            if (!string.IsNullOrEmpty(this.Extension))
            {
                hashCode += this.Extension.GetHashCode();
            }
            if (!string.IsNullOrEmpty(this.ProgId))
            {
                hashCode += this.ProgId.GetHashCode();
            }
            if (!string.IsNullOrEmpty(this.ExecutableFilePath))
            {
                hashCode += this.ExecutableFilePath.GetHashCode();
            }
            return hashCode;
        }

        public static bool operator ==(FileAssociation a, FileAssociation b)
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

        public static bool operator !=(FileAssociation a, FileAssociation b)
        {
            return !(a == b);
        }
    }
}
