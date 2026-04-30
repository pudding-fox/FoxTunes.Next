using FoxTunes.Interfaces;
using RadioBrowserWrapper.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FoxTunes
{
    public class RadioBrowserReport : ReportComponent
    {
        public RadioBrowserReport(RadioBrowserBehaviour behaviour, IEnumerable<Station> stations)
        {
            this.Behaviour = behaviour;
            this.Stations = stations;
        }

        public RadioBrowserBehaviour Behaviour { get; private set; }

        public IEnumerable<Station> Stations { get; private set; }

        public override string Title
        {
            get
            {
                return Strings.RadioBrowserBehaviour_Path;
            }
        }

        public override string Description
        {
            get
            {
                return string.Empty;
            }
        }

        public override string[] Headers
        {
            get
            {
                return new[]
                {
                        nameof(Station.Name),
                        nameof(Station.UrlResolved)
                    };
            }
        }

        public override IEnumerable<IReportComponentRow> Rows
        {
            get
            {
                foreach (var station in this.Stations)
                {
                    yield return new RadioBrowserReportRow(this, station);
                }
            }
        }

        public class RadioBrowserReportRow : ReportComponentRow
        {
            public RadioBrowserReportRow(RadioBrowserReport report, Station station)
            {
                this.Report = report;
                this.Station = station;
            }

            public RadioBrowserReport Report { get; private set; }

            public Station Station { get; private set; }

            public override string[] Values
            {
                get
                {
                    return new[]
                    {
                            this.Station.Name,
                            this.Station.UrlResolved
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
                        return this.Report.Behaviour.AddStationsToPlaylist(new[] { this.Station });
                }
                return base.InvokeAsync(component);
            }
        }
    }
}
