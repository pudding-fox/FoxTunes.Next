using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FoxTunes
{
    [Serializable]
    public class ScannerItem
    {
        const int ERROR_CAPACITY = 10;

        private ScannerItem()
        {
            this.Id = Guid.NewGuid();
            this._Errors = new List<string>(ERROR_CAPACITY);
        }

        public const int PROGRESS_NONE = 0;

        public const int PROGRESS_COMPLETE = 100;

        public Guid Id { get; private set; }

        public string FileName { get; private set; }

        public string GroupName { get; private set; }

        public double ItemPeak { get; set; }

        public double ItemGain { get; set; }

        public double GroupPeak { get; set; }

        public double GroupGain { get; set; }

        private IList<string> _Errors { get; set; }

        public IEnumerable<string> Errors
        {
            get
            {
                return this._Errors;
            }
        }

        public int Progress { get; set; }

        public ScannerItemStatus Status { get; set; }

        public ReplayGainMode Mode { get; set; }

        public void AddError(string error)
        {
            this._Errors.Add(error);
            if (this._Errors.Count > ERROR_CAPACITY)
            {
                this._Errors.RemoveAt(0);
            }
        }

        public static ScannerItem FromFileData(IFileData fileData, ReplayGainMode mode)
        {
            var scannerItem = new ScannerItem()
            {
                FileName = fileData.FileName,
                Mode = mode
            };
            if (mode == ReplayGainMode.Album)
            {
                var parts = new List<string>();
                lock (fileData.MetaDatas)
                {
                    var metaDatas = fileData.MetaDatas.ToDictionary(
                        element => element.Name,
                        StringComparer.OrdinalIgnoreCase
                    );
                    var metaDataItem = default(MetaDataItem);
                    if (metaDatas.TryGetValue(CommonMetaData.Year, out metaDataItem))
                    {
                        parts.Add(metaDataItem.Value);
                    }
                    if (metaDatas.TryGetValue(CommonMetaData.Album, out metaDataItem))
                    {
                        parts.Add(metaDataItem.Value);
                    }
                }
                scannerItem.GroupName = string.Join(" - ", parts);
            }
            return scannerItem;
        }
    }

    public enum ScannerItemStatus : byte
    {
        None,
        Processing,
        Complete,
        Cancelled,
        Failed
    }
}
