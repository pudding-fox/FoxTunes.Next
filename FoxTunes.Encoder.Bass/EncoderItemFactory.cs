using FoxTunes.Interfaces;
using System;
using System.IO;
using System.Linq;

namespace FoxTunes
{
    public class EncoderItemFactory : BaseFactory
    {
        public static readonly object SyncRoot = new object();

        public EncoderItemFactory(IEncoderOutputPath outputPath)
        {
            this.OutputPath = outputPath;
        }

        public IEncoderOutputPath OutputPath { get; private set; }

        public BassEncoderSettingsFactory SettingsFactory { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.SettingsFactory = ComponentRegistry.Instance.GetComponent<BassEncoderSettingsFactory>();
            base.InitializeComponent(core);
        }

        public EncoderItem[] Create(IFileData[] fileDatas, string profile)
        {
            return fileDatas
                .OrderBy(fileData => fileData.FileName)
                .Select(fileData => Create(fileDatas, fileData, profile))
                .ToArray();
        }

        public EncoderItem Create(IFileData[] fileDatas, IFileData fileData, string profile)
        {
            var outputFileName = this.GetOutputFileName(fileDatas, fileData, profile);
            return EncoderItem.Create(fileData.FileName, outputFileName, fileData.MetaDatas, profile);
        }

        protected virtual string GetOutputFileName(IFileData[] fileDatas, IFileData fileData, string profile)
        {
            var settings = this.SettingsFactory.CreateSettings(profile);
            var extension = settings.Extension;
            var name = default(string);
            if (FileSystemHelper.IsLocalPath(fileData.FileName))
            {
                name = Path.GetFileNameWithoutExtension(fileData.FileName);
            }
            else
            {
                lock (fileData.MetaDatas)
                {
                    var metaData = fileData.MetaDatas.ToDictionary2();
                    var track = default(string);
                    var title = default(string);
                    if (!metaData.TryGetValue(CommonMetaData.Track, out track))
                    {
                        track = Convert.ToString(fileDatas.IndexOf(fileData) + 1);
                    }
                    if (!metaData.TryGetValue(CommonMetaData.Title, out title))
                    {
                        name = string.Format("Track {0:00}", track);
                    }
                    else
                    {
                        name = string.Format("{0:00} {1}", track, title);
                    }
                    name = name.Replace(Path.GetInvalidFileNameChars(), '_');
                }
            }
            var fileName = string.Format("{0}.{1}", name, extension);
            var directoryName = this.OutputPath.GetDirectoryName(fileData);
            return Path.Combine(directoryName, fileName);
        }
    }
}
