using System;
using System.IO;

namespace FoxTunes.Interfaces
{
    public interface IFileAbstraction : IDisposable
    {
        string FileName { get; }

        string DirectoryName { get; }

        string FileExtension { get; }

        long FileSize { get; }

        DateTime FileCreationTime { get; }

        DateTime FileModificationTime { get; }

        Stream ReadStream { get; }

        Stream WriteStream { get; }

        void CloseStream(Stream stream);
    }
}
