using FoxTunes.Interfaces;
using MD.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static FoxTunes.MinidiscTrackFactory;

namespace FoxTunes
{
    public class MinidiscReport : ReportComponent
    {
        public MinidiscReport(MinidiscBehaviour behaviour, IDevice device, IDisc currentDisc, IDisc updatedDisc)
            : this(behaviour, device, currentDisc, updatedDisc.Title, GetActions(currentDisc, updatedDisc))
        {

        }

        public MinidiscReport(MinidiscBehaviour behaviour, IDevice device, IDisc disc, string discTitle, IDictionary<ITrack, TrackAction> tracks)
        {
            this.Behaviour = behaviour;
            this.Device = device;
            this.Disc = disc;
            this.DiscTitle = discTitle;
            this.Tracks = tracks;
        }

        public MinidiscBehaviour Behaviour { get; private set; }

        public IDevice Device { get; private set; }

        public IDisc Disc { get; private set; }

        public string DiscTitle { get; private set; }

        public IDictionary<ITrack, TrackAction> Tracks { get; private set; }

        public override string Title
        {
            get
            {
                var disc = GetUpdatedDisc(this.Disc, this.DiscTitle, this.Tracks);
                return string.Format("{0} (Capacity {1}%, UTOC {2}%)", this.DiscTitle, disc.GetCapacity().PercentUsed, disc.GetUTOC().PercentUsed);
            }
        }

        public override string Description
        {
            get
            {
                var builder = new StringBuilder();
                if (!string.Equals(this.Disc.Title, this.DiscTitle, StringComparison.OrdinalIgnoreCase))
                {
                    builder.AppendFormat("Updating title: {0}", this.DiscTitle);
                    builder.AppendLine();
                }
                else
                {
                    builder.AppendFormat("Keeping title: {0}", this.DiscTitle);
                    builder.AppendLine();
                }
                foreach (var pair in this.Tracks)
                {
                    switch (pair.Value.Action)
                    {
                        case TrackAction.NONE:
                            builder.AppendFormat("Keeping track ({0}, {1}): {2}", pair.Key.Position, Enum.GetName(typeof(Compression), pair.Key.Compression), pair.Key.Name);
                            builder.AppendLine();
                            break;
                        case TrackAction.ADD:
                            builder.AppendFormat("Adding track ({0}, {1}): {2}", pair.Key.Position, Enum.GetName(typeof(Compression), pair.Key.Compression), pair.Key.Name);
                            builder.AppendLine();
                            break;
                        case TrackAction.UPDATE:
                            if (!string.Equals(pair.Key.Name, pair.Value.Name, StringComparison.OrdinalIgnoreCase))
                            {
                                builder.AppendFormat("Updating track ({0}, {1}): {2} => {3}", pair.Key.Position, Enum.GetName(typeof(Compression), pair.Key.Compression), pair.Key.Name, pair.Value.Name);
                            }
                            else
                            {
                                builder.AppendFormat("Keeping track ({0}, {1}): {2}", pair.Key.Position, Enum.GetName(typeof(Compression), pair.Key.Compression), pair.Key.Name);
                            }
                            builder.AppendLine();
                            break;
                        case TrackAction.REMOVE:
                            builder.AppendFormat("Removing track ({0}, {1}): {2}", pair.Key.Position, Enum.GetName(typeof(Compression), pair.Key.Compression), pair.Key.Name);
                            builder.AppendLine();
                            break;
                    }
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
                    Strings.MinidiscReport_Name,
                    Strings.MinidiscReport_Time,
                    Strings.MinidiscReport_Format,
                    Strings.MinidiscReport_Action
                };
            }
        }

        public override IEnumerable<IReportComponentRow> Rows
        {
            get
            {
                foreach (var pair in this.Tracks.OrderBy(_pair => _pair.Key.Position))
                {
                    yield return new MinidiscReportRow(this, pair.Key, pair.Value);
                }
            }
        }

        public bool HasChanges
        {
            get
            {
                return this.Tracks.Any(pair => pair.Value.Action != TrackAction.NONE);
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
                if (this.HasChanges)
                {
                    yield return new InvocationComponent(InvocationComponent.CATEGORY_REPORT, ACCEPT, Strings.MinidiscReport_Write, attributes: InvocationComponent.ATTRIBUTE_SYSTEM);
                }
            }
        }

        public IUserInterface UserInterface { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.UserInterface = core.Components.UserInterface;
            base.InitializeComponent(core);
        }

        public override Task InvokeAsync(IInvocationComponent component)
        {
            switch (component.Id)
            {
                case ACCEPT:
                    return this.Write();
            }
            return base.InvokeAsync(component);
        }

        protected virtual async Task Write()
        {
            var currentDisc = this.Disc;
            var updatedDisc = GetUpdatedDisc(currentDisc, this.DiscTitle, this.Tracks);
            var success = await this.Behaviour.WriteDisc(this.Device, currentDisc, updatedDisc).ConfigureAwait(false);
            if (!success)
            {
                //If disc was not written then show this report again.
                //TODO: If some data was written then our changes need re-calculating.
                await this.Behaviour.ReportEmitter.Send(this).ConfigureAwait(false);
            }
        }

        public void Rename(ITrack track)
        {
            var name = this.UserInterface.Prompt(Strings.MinidiscReport_Rename, track.Name);
            if (string.IsNullOrEmpty(name))
            {
                return;
            }
            if (string.Equals(track.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
            var current = default(TrackAction);
            if (this.Tracks.TryGetValue(track, out current) && current.Action == TrackAction.ADD)
            {
                track.Name = name;
            }
            else
            {
                this.Tracks[track] = new TrackAction(track, name);
            }
            this.OnTitleChanged();
            this.OnDescriptionChanged();
            this.OnRowsChanged();
        }

        public void Update(ITrack track, byte action)
        {
            var current = default(TrackAction);
            if (this.Tracks.TryGetValue(track, out current) && current.Action == TrackAction.ADD && action == TrackAction.REMOVE)
            {
                //Cancel adding new track.
                this.Tracks.Remove(track);
            }
            else
            {
                this.Tracks[track] = new TrackAction(track, action);
            }
            this.OnTitleChanged();
            this.OnDescriptionChanged();
            this.OnRowsChanged();
        }

        public class MinidiscReportRow : ReportComponentRow
        {
            const string RENAME = "AAAA";

            const string REMOVE = "BBBB";

            const string KEEP = "CCCC";

            public MinidiscReportRow(MinidiscReport report, ITrack track, TrackAction action)
            {
                this.Report = report;
                this.Track = track;
                this.Action = action;
            }

            public MinidiscReport Report { get; private set; }

            public ITrack Track { get; private set; }

            public TrackAction Action { get; private set; }

            public override string[] Values
            {
                get
                {
                    return new[]
                    {
                        GetTrackName(this.Track, this.Action),
                        this.Track.Time.ToString(@"mm\:ss"),
                        GetFormatName(this.Track.Compression),
                        GetActionName(this.Action)
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
                    switch (this.Action.Action)
                    {
                        case TrackAction.NONE:
                        case TrackAction.ADD:
                            yield return new InvocationComponent(InvocationComponent.CATEGORY_REPORT, RENAME, Strings.MinidiscReportRow_Rename);
                            yield return new InvocationComponent(InvocationComponent.CATEGORY_REPORT, REMOVE, Strings.MinidiscReportRow_Remove);
                            break;
                        case TrackAction.UPDATE:
                            yield return new InvocationComponent(InvocationComponent.CATEGORY_REPORT, RENAME, Strings.MinidiscReportRow_Rename);
                            yield return new InvocationComponent(InvocationComponent.CATEGORY_REPORT, KEEP, Strings.MinidiscReportRow_Keep);
                            break;
                        case TrackAction.REMOVE:
                            yield return new InvocationComponent(InvocationComponent.CATEGORY_REPORT, KEEP, Strings.MinidiscReportRow_Keep);
                            break;
                    }
                }
            }

            public override Task InvokeAsync(IInvocationComponent component)
            {
                switch (component.Id)
                {
                    case RENAME:
                        this.Report.Rename(this.Track);
                        break;
                    case REMOVE:
                        this.Report.Update(this.Track, TrackAction.REMOVE);
                        break;
                    case KEEP:
                        this.Report.Update(this.Track, TrackAction.NONE);
                        break;
                }
                return base.InvokeAsync(component);
            }
        }

        public static IDictionary<ITrack, TrackAction> GetActions(IDisc currentDisc, IDisc updatedDisc)
        {
            var actions = new Dictionary<ITrack, TrackAction>();
            foreach (var currentTrack in currentDisc.Tracks)
            {
                var updatedTrack = updatedDisc.Tracks.GetTrack(currentTrack);
                if (updatedTrack != null)
                {
                    //Nothing to do.
                }
                else
                {
                    actions.Add(currentTrack, new TrackAction(currentTrack, TrackAction.REMOVE));
                }
            }
            foreach (var updatedTrack in updatedDisc.Tracks)
            {
                var currentTrack = currentDisc.Tracks.GetTrack(updatedTrack);
                if (currentTrack != null)
                {
                    if (!MinidiscTrackFactory.StringComparer.Instance.Equals(currentTrack.Name, updatedTrack.Name))
                    {
                        actions.Add(currentTrack, new TrackAction(updatedTrack, updatedTrack.Name));
                    }
                    else
                    {
                        actions.Add(currentTrack, new TrackAction(updatedTrack, TrackAction.NONE));
                    }
                }
                else
                {
                    actions.Add(updatedTrack, new TrackAction(updatedTrack, TrackAction.ADD));
                }
            }
            return actions;
        }

        public static IDisc GetUpdatedDisc(IDisc currentDisc, string title, IDictionary<ITrack, TrackAction> tracks)
        {
            Logger.Write(typeof(MinidiscReport), LogLevel.Debug, "Getting updated disc..");
            var updatedDisc = currentDisc.Clone();
            if (!string.Equals(updatedDisc.Title, title, StringComparison.OrdinalIgnoreCase))
            {
                Logger.Write(typeof(MinidiscReport), LogLevel.Debug, "Updating title: {0}", title);
                updatedDisc.Title = title;
            }
            else
            {
                Logger.Write(typeof(MinidiscReport), LogLevel.Debug, "Keeping title: {0}", title);
            }
            foreach (var pair in tracks.OrderBy(_pair => _pair.Key.Position))
            {
                switch (pair.Value.Action)
                {
                    case TrackAction.NONE:
                        Logger.Write(typeof(MinidiscReport), LogLevel.Debug, "Keeping track ({0}, {1}): {2}", pair.Key.Position, Enum.GetName(typeof(Compression), pair.Key.Compression), pair.Key.Name);
                        break;
                    case TrackAction.ADD:
                        Logger.Write(typeof(MinidiscReport), LogLevel.Debug, "Adding track ({0}, {1}): {2}", pair.Key.Position, Enum.GetName(typeof(Compression), pair.Key.Compression), pair.Key.Name);
                        updatedDisc.Tracks.Add(pair.Key);
                        break;
                    case TrackAction.UPDATE:
                        var track = updatedDisc.Tracks.GetTrack(pair.Key);
                        if (track == null)
                        {
                            Logger.Write(typeof(MinidiscReport), LogLevel.Warn, "Failed to update track ({0}, {1}): {2}", pair.Key.Position, Enum.GetName(typeof(Compression), pair.Key.Compression), pair.Key.Name);
                        }
                        else if (!string.Equals(track.Name, pair.Value.Name, StringComparison.OrdinalIgnoreCase))
                        {
                            Logger.Write(typeof(MinidiscReport), LogLevel.Debug, "Updating track ({0}, {1}): {2} => {3}", pair.Key.Position, Enum.GetName(typeof(Compression), pair.Key.Compression), pair.Key.Name, pair.Value.Name);
                            track.Name = pair.Value.Name;
                        }
                        break;
                    case TrackAction.REMOVE:
                        Logger.Write(typeof(MinidiscReport), LogLevel.Debug, "Removing track ({0}, {1}): {2}", pair.Key.Position, Enum.GetName(typeof(Compression), pair.Key.Compression), pair.Key.Name);
                        updatedDisc.Tracks.Remove(pair.Key);
                        break;
                }
            }
            return updatedDisc;
        }

        public static string GetTrackName(ITrack track, TrackAction action)
        {
            switch (action.Action)
            {
                case TrackAction.UPDATE:
                    if (!string.IsNullOrEmpty(action.Name))
                    {
                        return action.Name;
                    }
                    break;
            }
            return track.Name;
        }

        public static string GetFormatName(Compression compression)
        {
            var name = Enum.GetName(typeof(Compression), compression);
            var localized = Strings.ResourceManager.GetString(string.Format("{0}.{1}", typeof(Compression).Name, name));
            if (!string.IsNullOrEmpty(localized))
            {
                return localized;
            }
            return name;
        }

        public static string GetActionName(TrackAction action)
        {
            var name = default(string);
            switch (action.Action)
            {
                case TrackAction.NONE:
                    name = "TrackAction.None";
                    break;
                case TrackAction.ADD:
                    name = "TrackAction.Add";
                    break;
                case TrackAction.UPDATE:
                    name = "TrackAction.Update";
                    break;
                case TrackAction.REMOVE:
                    name = "TrackAction.Remove";
                    break;
            }
            var localized = Strings.ResourceManager.GetString(name);
            if (!string.IsNullOrEmpty(localized))
            {
                return localized;
            }
            return name;
        }

        public class TrackAction
        {
            public const byte NONE = 0;

            public const byte ADD = 1;

            public const byte UPDATE = 2;

            public const byte REMOVE = 4;

            public TrackAction(ITrack track, byte action)
            {
                this.Track = track;
                this.Name = track.Name;
                this.Action = action;
            }

            public TrackAction(ITrack track, string name)
            {
                this.Track = track;
                this.Name = name;
                this.Action = UPDATE;
            }

            public ITrack Track { get; private set; }

            public string Name { get; private set; }

            public byte Action { get; private set; }
        }
    }
}
