using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoxTunes
{
    public abstract class AIPlaylistTaskBase : PlaylistTaskBase
    {
        protected AIPlaylistTaskBase(Playlist playlist, int sequence = 0) : base(playlist, sequence)
        {
        }

        public IReportEmitter ReportEmitter { get; private set; }

        public BooleanConfigurationElement Report { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            base.InitializeComponent(core);
            this.ReportEmitter = core.Components.ReportEmitter;
            this.Report = core.Components.Configuration.GetElement<BooleanConfigurationElement>(
                AIBehaviourConfiguration.SECTION,
                AIBehaviourConfiguration.REPORT
            );
        }

        protected virtual async Task<IEnumerable<string>> GetPathsFromResponse(string response)
        {
            Logger.Write(this, LogLevel.Debug, "Extracting tracks from response.");
            var paths = new OrderedDictionary();
            using (var reader = new StringReader(response))
            {
                var line = default(string);
                while ((line = await reader.ReadLineAsync().ConfigureAwait(false)) != null)
                {
                    var parts = line.Split('\t');
                    if (parts.Length == 1)
                    {
                        var fileName = parts[0];
                        Logger.Write(this, LogLevel.Debug, "Got file from response (low confidence): {0}", fileName);
                        paths[fileName] = null;
                    }
                    else if (parts.Length == 2)
                    {
                        var fileName = parts[0];
                        var reason = parts[1];
                        Logger.Write(this, LogLevel.Debug, "Got file from response: {0}, {1}", fileName, reason);
                        paths[fileName] = reason;
                    }
                }
                if (this.Report.Value)
                {
                    this.Dispatch(() => this.ReportEmitter.Send(new AIPlaylistReport(paths)));
                }
                return paths.Keys.Cast<string>();
            }
        }

        public class AIPlaylistReport : ReportComponent
        {
            public AIPlaylistReport(OrderedDictionary paths)
            {
                this.Paths = paths;
            }

            public OrderedDictionary Paths { get; private set; }

            public override string Title
            {
                get
                {
                    return Strings.AIPlaylistReport_Name;
                }
            }

            public override string Description
            {
                get
                {
                    var builder = new StringBuilder();
                    foreach (var key in this.Paths.Keys)
                    {
                        builder.AppendLine(string.Format("{0} => {1}", key, this.Paths[key]));
                    }
                    return builder.ToString();
                }
            }

            public override string[] Headers
            {
                get
                {
                    return new[]
                    {
                        Strings.AIPlaylistReport_Track,
                        Strings.AIPlaylistReport_Reason
                    };
                }
            }

            public override IEnumerable<IReportComponentRow> Rows
            {
                get
                {
                    foreach (var key in this.Paths.Keys)
                    {
                        yield return new AIPlaylistReportRow(
                            this,
                            Convert.ToString(key),
                            Convert.ToString(this.Paths[key])
                        );
                    }
                }
            }

            public class AIPlaylistReportRow : ReportComponentRow
            {
                public AIPlaylistReportRow(AIPlaylistReport report, string fileName, string reason)
                {
                    this.Report = report;
                    this.FileName = fileName;
                    this.Reason = reason;
                }

                public AIPlaylistReport Report { get; private set; }

                public string FileName { get; private set; }

                public string Reason { get; private set; }

                public override string[] Values
                {
                    get
                    {
                        return new[]
                        {
                            Path.GetFileName(this.FileName),
                            this.Reason
                        };
                    }
                }
            }
        }
    }
}
