using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoxTunes
{
    public class BassEncoderReport : ReportComponent
    {
        public BassEncoderReport(EncoderItem[] encoderItems)
        {
            this.EncoderItems = encoderItems;
        }

        public EncoderItem[] EncoderItems { get; private set; }

        public override string Title
        {
            get
            {
                return "Encoder Output";
            }
        }

        public override string Description
        {
            get
            {
                return string.Join(
                    Environment.NewLine,
                    this.EncoderItems.Select(encoderItem => this.GetDescription(encoderItem))
                );
            }
        }

        protected virtual string GetDescription(EncoderItem encoderItem)
        {
            var builder = new StringBuilder();
            builder.AppendFormat(
                "{0} -> {1}",
                encoderItem.InputFileName,
                encoderItem.OutputFileName
            );
            if (encoderItem.Status != EncoderItemStatus.Complete && encoderItem.Errors.Any())
            {
                builder.AppendLine(" -> Error");
                foreach (var error in encoderItem.Errors)
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
                    "Codec",
                    "Status"
                };
            }
        }

        public override IEnumerable<IReportComponentRow> Rows
        {
            get
            {
                return this.EncoderItems.Select(encoderItem => new BassEncoderReportRow(this, encoderItem));
            }
        }

        public IFileSystemBrowser FileSystemBrowser { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.FileSystemBrowser = core.Components.FileSystemBrowser;
            base.InitializeComponent(core);
        }

        public class BassEncoderReportRow : ReportComponentRow
        {
            public BassEncoderReportRow(BassEncoderReport report, EncoderItem encoderItem)
            {
                this.Report = report;
                this.EncoderItem = encoderItem;
            }

            public BassEncoderReport Report { get; private set; }

            public EncoderItem EncoderItem { get; private set; }

            public override string[] Values
            {
                get
                {
                    if (!string.IsNullOrEmpty(this.EncoderItem.OutputFileName))
                    {
                        return new[]
                        {
                            this.EncoderItem.OutputFileName,
                            Path.GetExtension(this.EncoderItem.OutputFileName).TrimStart('.').ToUpper(),
                            Enum.GetName(typeof(EncoderItemStatus), this.EncoderItem.Status)
                        };
                    }
                    else
                    {
                        return new[]
                        {
                            this.EncoderItem.InputFileName,
                            Path.GetExtension(this.EncoderItem.InputFileName).TrimStart('.').ToUpper(),
                            Enum.GetName(typeof(EncoderItemStatus), this.EncoderItem.Status)
                        };
                    }
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
                    if (File.Exists(this.EncoderItem.OutputFileName))
                    {
                        yield return new InvocationComponent(InvocationComponent.CATEGORY_REPORT, ACTIVATE, attributes: InvocationComponent.ATTRIBUTE_SYSTEM);
                    }
                }
            }

            public override Task InvokeAsync(IInvocationComponent component)
            {
                switch (component.Id)
                {
                    case ACTIVATE:
                        this.Report.FileSystemBrowser.Select(this.EncoderItem.OutputFileName);
                        break;
                }
                return base.InvokeAsync(component);
            }
        }
    }
}
