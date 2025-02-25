using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoxTunes
{
    public class ReleaseSelectionReport : ReportComponent
    {
        public ReleaseSelectionReport(Discogs.ReleaseLookup releaseLookup, Discogs.Release[] releases)
        {
            this.ReleaseLookup = releaseLookup;
            this.Releases = releases;
        }

        public Discogs.ReleaseLookup ReleaseLookup { get; private set; }

        public Discogs.Release[] Releases { get; private set; }

        public ReleaseSelectionReportRow SelectedRow { get; private set; }

        public Discogs.Release SelectedRelease { get; private set; }

        public IUserInterface UserInterface { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.UserInterface = core.Components.UserInterface;
            base.InitializeComponent(core);
        }

        public override string Title
        {
            get
            {
                return string.Format(Strings.ReleaseSelectionReport_Title, this.ReleaseLookup.Artist, this.ReleaseLookup.Album, this.ReleaseLookup.FileDatas.Length);
            }
        }

        public override string Description
        {
            get
            {
                return string.Join(
                    Environment.NewLine,
                    this.Releases.Select(
                        release => this.GetDescription(release)
                    )
                );
            }
        }

        protected virtual string GetDescription(Discogs.Release release)
        {
            var builder = new StringBuilder();
            builder.AppendFormat("{0} - {1} - {2}", release.Id, release.Title, release.Url);
            return builder.ToString();
        }

        public override string[] Headers
        {
            get
            {
                return new[]
                {
                    Strings.ReleaseSelectionReport_ReleaseId,
                    Strings.ReleaseSelectionReport_ReleaseTitle,
                    Strings.ReleaseSelectionReport_ReleaseUrl
                };
            }
        }

        public override IEnumerable<IReportComponentRow> Rows
        {
            get
            {
                return this.Releases.Select(release => new ReleaseSelectionReportRow(this, release));
            }
        }

        public override bool IsDialog
        {
            get
            {
                return true;
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
                yield return new InvocationComponent(InvocationComponent.CATEGORY_REPORT, ACCEPT, Strings.ReleaseSelectionReport_Confirm, attributes: InvocationComponent.ATTRIBUTE_SYSTEM);
            }
        }

        public override Task InvokeAsync(IInvocationComponent component)
        {
            switch (component.Id)
            {
                case ACCEPT:
                    if (this.SelectedRow != null)
                    {
                        this.SelectedRelease = this.SelectedRow.Release;
                    }
                    break;
            }
            return base.InvokeAsync(component);
        }

        public class ReleaseSelectionReportRow : ReportComponentRow
        {
            public ReleaseSelectionReportRow(ReleaseSelectionReport report, Discogs.Release release)
            {
                this.Report = report;
                this.Release = release;
            }

            public ReleaseSelectionReport Report { get; private set; }

            public Discogs.Release Release { get; private set; }

            public override string[] Values
            {
                get
                {
                    return new[]
                    {
                        this.Release.Id,
                        this.Release.Title,
                        this.Release.Url
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
                    yield return new InvocationComponent(InvocationComponent.CATEGORY_REPORT, SELECT, attributes: InvocationComponent.ATTRIBUTE_SYSTEM);
                    yield return new InvocationComponent(InvocationComponent.CATEGORY_REPORT, ACTIVATE, attributes: InvocationComponent.ATTRIBUTE_SYSTEM);
                }
            }

            public override Task InvokeAsync(IInvocationComponent component)
            {
                switch (component.Id)
                {
                    case SELECT:
                        this.Report.SelectedRow = this;
                        break;
                    case ACTIVATE:
                        var url = new Uri(new Uri("https://www.discogs.com"), this.Release.Url).ToString();
                        this.Report.UserInterface.OpenInShell(url);
                        break;
                }
                return base.InvokeAsync(component);
            }
        }
    }
}