using FoxTunes.Interfaces;
using System;
using System.IO;

namespace FoxTunes
{
    public class CueSheetFileAbstraction : BaseComponent, IFileAbstraction
    {
        public CueSheetFileAbstraction(string fileName, string entryName)
        {
            this.Info = new FileInfo(fileName);
            this.EntryName = entryName;
        }

        #region IFileAbstraction

        string IFileAbstraction.FileName
        {
            get
            {
                return this.EntryName;
            }
        }

        string IFileAbstraction.DirectoryName
        {
            get
            {
                return this.Info.DirectoryName;
            }
        }

        string IFileAbstraction.FileExtension
        {
            get
            {
                return this.Info.Extension;
            }
        }

        long IFileAbstraction.FileSize
        {
            get
            {
                return 0;
            }
        }

        DateTime IFileAbstraction.FileCreationTime
        {
            get
            {
                return this.Info.CreationTime;
            }
        }

        DateTime IFileAbstraction.FileModificationTime
        {
            get
            {
                return this.Info.LastWriteTime;
            }
        }

        public Stream ReadStream { get; private set; }

        public Stream WriteStream { get; private set; }

        public void CloseStream(Stream stream)
        {
            stream.Close();
        }

        #endregion

        public FileInfo Info { get; private set; }

        public string EntryName { get; private set; }

        public bool IsOpen { get; private set; }

        public void Open()
        {
            this.ReadStream = File.OpenRead(this.Info.FullName);
            this.IsOpen = true;
        }

        public void Close()
        {
            if (this.ReadStream != null)
            {
                this.ReadStream.Dispose();
                this.ReadStream = null;
            }
            this.IsOpen = false;
        }

        public bool IsDisposed { get; private set; }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (this.IsDisposed || !disposing)
            {
                return;
            }
            this.OnDisposing();
            this.IsDisposed = true;
        }

        protected virtual void OnDisposing()
        {
            this.Close();
        }

        ~CueSheetFileAbstraction()
        {
            Logger.Write(this.GetType(), LogLevel.Error, "Component was not disposed: {0}", this.GetType().Name);
            try
            {
                this.Dispose(true);
            }
            catch
            {
                //Nothing can be done, never throw on GC thread.
            }
        }

        public static CueSheetFileAbstraction Create(string fileName, string entryName)
        {
            var fileAbstraction = new CueSheetFileAbstraction(fileName, entryName);
            fileAbstraction.Open();
            return fileAbstraction;
        }
    }
}
