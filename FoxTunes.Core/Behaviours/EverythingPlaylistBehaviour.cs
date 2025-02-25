using FoxTunes.Interfaces;
using System;
using System.Threading.Tasks;

namespace FoxTunes
{
    [ComponentDependency(Slot = ComponentSlots.Database)]
    public class EverythingPlaylistBehaviour : PlaylistBehaviourBase
    {
        public override Func<Playlist, bool> Predicate
        {
            get
            {
                return playlist => playlist.Type == PlaylistType.Everything && playlist.Enabled;
            }
        }

        public ICore Core { get; private set; }

        public ILibraryManager LibraryManager { get; private set; }

        public ILibraryHierarchyBrowser LibraryHierarchyBrowser { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.Core = core;
            this.LibraryManager = core.Managers.Library;
            this.LibraryHierarchyBrowser = core.Components.LibraryHierarchyBrowser;
            this.LibraryHierarchyBrowser.FilterChanged += this.OnFilterChanged;
            base.InitializeComponent(core);
        }

        protected virtual void OnFilterChanged(object sender, EventArgs e)
        {
            this.Dispatch(() => this.Refresh(false));
        }

        protected override Task OnSignal(object sender, ISignal signal)
        {
            switch (signal.Name)
            {
                case CommonSignals.HierarchiesUpdated:
                    this.OnHierarchiesUpdated();
                    break;
            }
            return base.OnSignal(sender, signal);
        }

        protected virtual void OnHierarchiesUpdated()
        {
            this.Dispatch(() => this.Refresh(false));
        }

        public override async Task Refresh(Playlist playlist, bool force)
        {
            var libraryHierarchy = this.LibraryManager.SelectedHierarchy;
            var libraryHierarchyNodes = this.LibraryHierarchyBrowser.GetNodes(libraryHierarchy);
            using (var task = new AddLibraryHierarchyNodesToPlaylistTask(playlist, 0, libraryHierarchyNodes, this.LibraryHierarchyBrowser.Filter, true))
            {
                task.InitializeComponent(this.Core);
                await task.Run().ConfigureAwait(false);
            }
        }
    }
}
