using FoxTunes.Interfaces;
using System;
using System.IO;
using System.Linq;

namespace FoxTunes
{
    public class BassEncoderOutputPath : BaseComponent, IEncoderOutputPath
    {
        public static readonly object SyncRoot = new object();

        public IFileSystemBrowser FileSystemBrowser { get; private set; }

        public BassEncoderOutputDestination Destination { get; private set; }

        public string SpecificFolder { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.FileSystemBrowser = core.Components.FileSystemBrowser;
            core.Components.Configuration.GetElement<SelectionConfigurationElement>(
                BassEncoderBehaviourConfiguration.SECTION,
                BassEncoderBehaviourConfiguration.DESTINATION_ELEMENT
            ).ConnectValue(value => this.Destination = BassEncoderBehaviourConfiguration.GetDestination(value));
            core.Components.Configuration.GetElement<TextConfigurationElement>(
                BassEncoderBehaviourConfiguration.SECTION,
                BassEncoderBehaviourConfiguration.DESTINATION_LOCATION_ELEMENT
            ).ConnectValue(value => this.SpecificFolder = value);
            base.InitializeComponent(core);
        }

        public string GetDirectoryName(IFileData fileData)
        {
            var directoryName = default(string);
            switch (this.Destination)
            {
                default:
                case BassEncoderOutputDestination.Browse:
                    if (!this.GetBrowseFolder(fileData.FileName, out directoryName))
                    {
                        throw new OperationCanceledException();
                    }
                    break;
                case BassEncoderOutputDestination.Source:
                    //TODO: I think we can always use fileData.DirectoryName
                    if (FileSystemHelper.IsLocalPath(fileData.FileName))
                    {
                        directoryName = Path.GetDirectoryName(fileData.FileName);
                    }
                    else
                    {
                        directoryName = fileData.DirectoryName;
                    }
                    break;
                case BassEncoderOutputDestination.Specific:
                    directoryName = this.SpecificFolder;
                    break;
            }
            if (!this.CanWrite(directoryName))
            {
                throw new InvalidOperationException(string.Format("Cannot output to path \"{0}\" please check encoder settings.", directoryName));
            }
            return directoryName;
        }

        private string _BrowseFolder { get; set; }

        protected virtual bool GetBrowseFolder(string fileName, out string directoryName)
        {
            lock (SyncRoot)
            {
                if (string.IsNullOrEmpty(this._BrowseFolder))
                {
                    var path = default(string);
                    if (FileSystemHelper.IsLocalPath(fileName))
                    {
                        path = Path.GetDirectoryName(fileName);
                    }
                    var options = new BrowseOptions(
                        "Save As",
                        path,
                        Enumerable.Empty<BrowseFilter>(),
                        BrowseFlags.Folder
                    );
                    var result = this.FileSystemBrowser.Browse(options);
                    if (!result.Success)
                    {
                        Logger.Write(this, LogLevel.Debug, "Save As folder browse dialog was cancelled.");
                        directoryName = null;
                        return false;
                    }
                    this._BrowseFolder = result.Paths.FirstOrDefault();
                    Logger.Write(this, LogLevel.Debug, "Browse folder: {0}", this._BrowseFolder);
                }
            }
            directoryName = this._BrowseFolder;
            return true;
        }

        protected virtual bool CanWrite(string directoryName)
        {
            var uri = default(Uri);
            if (!Uri.TryCreate(directoryName, UriKind.Absolute, out uri))
            {
                return false;
            }
            return string.Equals(uri.Scheme, Uri.UriSchemeFile, StringComparison.OrdinalIgnoreCase);
        }

        public class Fixed : IEncoderOutputPath
        {
            public Fixed(string directoryName)
            {
                this.DirectoryName = directoryName;
            }

            public string DirectoryName { get; private set; }

            public string GetDirectoryName(IFileData fileData)
            {
                return this.DirectoryName;
            }
        }
    }
}
