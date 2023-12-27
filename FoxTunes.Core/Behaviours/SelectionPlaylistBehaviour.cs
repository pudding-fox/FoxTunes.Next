using FoxTunes.Interfaces;
using System;
using System.Threading.Tasks;

namespace FoxTunes
{
    [ComponentDependency(Slot = ComponentSlots.Database)]
    public class SelectionPlaylistBehaviour : PlaylistBehaviourBase
    {
        public override Func<Playlist, bool> Predicate
        {
            get
            {
                return playlist => playlist.Type == PlaylistType.Selection && playlist.Enabled;
            }
        }

        public ICore Core { get; private set; }

        public ILibraryManager LibraryManager { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.Core = core;
            this.LibraryManager = core.Managers.Library;
            this.LibraryManager.SelectedItemChanged += this.OnSelectedItemChanged;
            base.InitializeComponent(core);
        }

        protected virtual void OnSelectedItemChanged(object sender, EventArgs e)
        {
            this.Dispatch(() => this.Refresh(false));
        }

        public override async Task Refresh(Playlist playlist, bool force)
        {
            var libraryHierarchyNode = this.LibraryManager.SelectedItem;
            if (libraryHierarchyNode != null)
            {
                playlist.Name = libraryHierarchyNode.Value;
                await this.Update(playlist).ConfigureAwait(false);
                using (var task = new AddLibraryHierarchyNodeToPlaylistTask(playlist, 0, libraryHierarchyNode, true))
                {
                    task.InitializeComponent(this.Core);
                    await task.Run().ConfigureAwait(false);
                }
            }
        }

        protected override void OnDisposing()
        {
            if (this.LibraryManager != null)
            {
                this.LibraryManager.SelectedItemChanged -= this.OnSelectedItemChanged;
            }
            base.OnDisposing();
        }
    }
}
