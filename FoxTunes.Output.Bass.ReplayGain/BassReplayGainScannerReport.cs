using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoxTunes
{
    public class BassReplayGainScannerReport : ReportComponent
    {
        public BassReplayGainScannerReport(IFileData[] fileDatas, ScannerItem[] scannerItems)
        {
            this.FileDatas = fileDatas.ToDictionary(fileData => fileData.FileName, StringComparer.OrdinalIgnoreCase);
            this.ScannerItems = scannerItems;
        }

        public Dictionary<string, IFileData> FileDatas { get; private set; }

        public ScannerItem[] ScannerItems { get; private set; }

        public override string Title
        {
            get
            {
                return Strings.BassReplayGainScannerReport_Title;
            }
        }

        public override string Description
        {
            get
            {
                return string.Join(
                    Environment.NewLine,
                    this.ScannerItems.Select(
                        scannerItem => this.GetDescription(scannerItem)
                    )
                );
            }
        }

        protected virtual string GetDescription(ScannerItem scannerItem)
        {
            var builder = new StringBuilder();
            builder.Append(scannerItem.FileName);
            if (scannerItem.Status != ScannerItemStatus.Complete && scannerItem.Errors.Any())
            {
                builder.AppendLine(" -> Error");
                foreach (var error in scannerItem.Errors)
                {
                    builder.AppendLine('\t' + error);
                }
            }
            else
            {
                builder.AppendLine(" -> OK");
            }
            return builder.ToString();
        }

        public override string[] Headers
        {
            get
            {
                return new[]
                {
                    "Path",
                    "Album",
                    "Album gain",
                    "Album peak",
                    "Track gain",
                    "Track peak",
                    "Status"
                };
            }
        }

        public override IEnumerable<IReportComponentRow> Rows
        {
            get
            {
                return this.ScannerItems.Select(scannerItem =>
                {
                    return new BassReplayGainScannerReportRow(this, this.FileDatas[scannerItem.FileName], scannerItem);
                });
            }
        }

        public IFileSystemBrowser FileSystemBrowser { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.FileSystemBrowser = core.Components.FileSystemBrowser;
            base.InitializeComponent(core);
        }

        public class BassReplayGainScannerReportRow : ReportComponentRow
        {
            public BassReplayGainScannerReportRow(BassReplayGainScannerReport report, IFileData fileData, ScannerItem scannerItem)
            {
                this.Report = report;
                this.FileData = fileData;
                this.ScannerItem = scannerItem;
            }

            public BassReplayGainScannerReport Report { get; private set; }

            public IFileData FileData { get; private set; }

            public ScannerItem ScannerItem { get; private set; }

            public override string[] Values
            {
                get
                {
                    var value = default(double);
                    var path = this.ScannerItem.FileName;
                    var album = this.ScannerItem.GroupName;
                    var albumGain = default(string);
                    var albumPeak = default(string);
                    var trackGain = default(string);
                    var trackPeak = default(string);
                    var status = Enum.GetName(typeof(ScannerItemStatus), this.ScannerItem.Status);
                    lock (this.FileData.MetaDatas)
                    {
                        var metaDatas = this.FileData.MetaDatas.ToDictionary(
                            element => element.Name,
                            StringComparer.OrdinalIgnoreCase
                        );
                        var metaDataItem = default(MetaDataItem);
                        if (metaDatas.TryGetValue(CommonMetaData.ReplayGainAlbumGain, out metaDataItem) && double.TryParse(metaDataItem.Value, out value))
                        {
                            albumGain = string.Format(
                                "{0}{1:0.00} dB",
                                value > 0 ? "+" : string.Empty,
                                value
                            );
                        }
                        else
                        {
                            albumGain = string.Empty;
                        }
                        if (metaDatas.TryGetValue(CommonMetaData.ReplayGainAlbumPeak, out metaDataItem) && double.TryParse(metaDataItem.Value, out value))
                        {
                            albumPeak = string.Format(
                                "{0:0.000000}",
                                value
                            );
                        }
                        else
                        {
                            albumPeak = string.Empty;
                        }
                        if (metaDatas.TryGetValue(CommonMetaData.ReplayGainTrackGain, out metaDataItem) && double.TryParse(metaDataItem.Value, out value))
                        {
                            trackGain = string.Format(
                                "{0}{1:0.00} dB",
                                value > 0 ? "+" : string.Empty,
                                value
                            );
                        }
                        else
                        {
                            trackGain = string.Empty;
                        }
                        if (metaDatas.TryGetValue(CommonMetaData.ReplayGainTrackPeak, out metaDataItem) && double.TryParse(metaDataItem.Value, out value))
                        {
                            trackPeak = string.Format(
                                "{0:0.000000}",
                                value
                            );
                        }
                        else
                        {
                            trackPeak = string.Empty;
                        }
                    }
                    return new[]
                    {
                        path,
                        album,
                        albumGain,
                        albumPeak,
                        trackGain,
                        trackPeak,
                        status
                    };
                }
            }

            public override IEnumerable<string> InvocationCategories
            {
                get
                {
                    yield return InvocationComponent.CATEGORY_REPORT;
                }
            }

            public override IEnumerable<IInvocationComponent> Invocations
            {
                get
                {
                    yield return new InvocationComponent(InvocationComponent.CATEGORY_REPORT, ACTIVATE, attributes: InvocationComponent.ATTRIBUTE_SYSTEM);
                }
            }

            public override Task InvokeAsync(IInvocationComponent component)
            {
                switch (component.Id)
                {
                    case ACTIVATE:
                        this.Report.FileSystemBrowser.Select(this.ScannerItem.FileName);
                        break;
                }
                return base.InvokeAsync(component);
            }
        }
    }
}
