using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FoxTunes
{
    [ComponentDependency(Slot = ComponentSlots.Database)]
    public class DynamicPlaylistBehaviour : PlaylistBehaviourBase
    {
        public const string Expression = "Expression";

        public const string DefaultExpression = "";

        public override Func<Playlist, bool> Predicate
        {
            get
            {
                return playlist => playlist.Type == PlaylistType.Dynamic && playlist.Enabled;
            }
        }

        protected virtual void GetConfig(Playlist playlist, out string expression)
        {
            var config = this.GetConfig(playlist);
            expression = config.GetValueOrDefault(Expression);
        }

        public ICore Core { get; private set; }

        public ILibraryManager LibraryManager { get; private set; }

        public ILibraryHierarchyBrowser LibraryHierarchyBrowser { get; private set; }

        public IFilterParser FilterParser { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.Core = core;
            this.LibraryManager = core.Managers.Library;
            this.LibraryHierarchyBrowser = core.Components.LibraryHierarchyBrowser;
            this.FilterParser = core.Components.FilterParser;
            base.InitializeComponent(core);
        }

        protected override async Task OnSignal(object sender, ISignal signal)
        {
            switch (signal.Name)
            {
                case CommonSignals.MetaDataUpdated:
                    await this.OnMetaDataUpdated(signal.State as MetaDataUpdatedSignalState).ConfigureAwait(false);
                    break;
                case CommonSignals.HierarchiesUpdated:
                    await this.Refresh(false).ConfigureAwait(false);
                    break;
            }
            await base.OnSignal(sender, signal).ConfigureAwait(false);
        }

        protected Task OnMetaDataUpdated(MetaDataUpdatedSignalState state)
        {
            if (state != null && state.Names != null)
            {
                return this.Refresh(state.Names);
            }
            else
            {
                return this.Refresh(Enumerable.Empty<string>());
            }
        }

        protected virtual async Task Refresh(IEnumerable<string> names)
        {
            foreach (var playlist in this.GetPlaylists())
            {
                var expression = default(string);
                this.GetConfig(playlist, out expression);
                if (string.IsNullOrEmpty(expression))
                {
                    continue;
                }
                if (names != null && names.Any())
                {
                    if (!this.FilterParser.AppliesTo(expression, names))
                    {
                        continue;
                    }
                }
                await this.Refresh(playlist, expression, false).ConfigureAwait(false);
            }
        }

        public override Task Refresh(Playlist playlist, bool force)
        {
            var expression = default(string);
            this.GetConfig(playlist, out expression);
            return this.Refresh(playlist, expression, force);
        }

        protected virtual async Task Refresh(Playlist playlist, string expression, bool force)
        {
            var libraryHierarchy = this.LibraryManager.SelectedHierarchy;
            if (libraryHierarchy == null || LibraryHierarchy.Empty.Equals(libraryHierarchy))
            {
                return;
            }
            var libraryHierarchyNodes = this.LibraryHierarchyBrowser.GetNodes(libraryHierarchy, expression);
            if (!libraryHierarchyNodes.Any())
            {
                Logger.Write(this, LogLevel.Debug, "Library search returned no results: {0}", expression);
                using (var task = new ClearPlaylistTask(playlist))
                {
                    task.InitializeComponent(this.Core);
                    await task.Run().ConfigureAwait(false);
                }
            }
            else
            {
                using (var task = new AddLibraryHierarchyNodesToPlaylistTask(playlist, 0, libraryHierarchyNodes, expression, true, false))
                {
                    task.InitializeComponent(this.Core);
                    await task.Run().ConfigureAwait(false);
                }
            }
        }
    }
}
