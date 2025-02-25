using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace FoxTunes.ViewModel
{
    public class LibraryBrowser : LibraryBase, IConfigurationTarget
    {
        private IConfiguration _Configuration { get; set; }

        public IConfiguration Configuration
        {
            get
            {
                return this._Configuration;
            }
            set
            {
                this._Configuration = value;
                this.OnConfigurationChanged();
            }
        }

        protected virtual void OnConfigurationChanged()
        {
            if (this.Configuration != null)
            {
                this.Configuration.GetElement<IntegerConfigurationElement>(
                    LibraryBrowserBaseConfiguration.SECTION,
                    LibraryBrowserBaseConfiguration.TILE_SIZE
                ).ConnectValue(value => this.TileSize = value);
                this.Configuration.GetElement<SelectionConfigurationElement>(
                    LibraryBrowserBaseConfiguration.SECTION,
                    LibraryBrowserBaseConfiguration.TILE_IMAGE
                ).ConnectValue(value => this.ImageMode = LibraryBrowserBaseConfiguration.GetLibraryImage(value));
            }
            if (this.ConfigurationChanged != null)
            {
                this.ConfigurationChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("Configuration");
        }

        public event EventHandler ConfigurationChanged;

        private ObservableCollection<LibraryBrowserFrame> _Frames { get; set; }

        public ObservableCollection<LibraryBrowserFrame> Frames
        {
            get
            {
                return this._Frames;
            }
            set
            {
                if (object.ReferenceEquals(this._Frames, value))
                {
                    return;
                }
                this.OnFramesChanging();
                this._Frames = value;
                this.OnFramesChanged();
            }
        }

        protected virtual void OnFramesChanging()
        {
            if (this._Frames == null)
            {
                return;
            }
            this._Frames.ForEach(frame => frame.Dispose());
        }

        protected virtual void OnFramesChanged()
        {
            if (this.Frames != null)
            {
                this.SelectedFrame = this.Frames.LastOrDefault();
            }
            else
            {
                this.SelectedFrame = null;
            }
            if (this.FramesChanged != null)
            {
                this.FramesChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("Frames");
        }

        public event EventHandler FramesChanged;

        private LibraryBrowserFrame _SelectedFrame { get; set; }

        public LibraryBrowserFrame SelectedFrame
        {
            get
            {
                return this._SelectedFrame;
            }
            set
            {
                if (object.ReferenceEquals(this._SelectedFrame, value))
                {
                    return;
                }
                this._SelectedFrame = value;
                this.OnSelectedFrameChanged();
            }
        }

        protected virtual void OnSelectedFrameChanged()
        {
            this.IsPlaceholderSelected = false;
            if (this.SelectedFrameChanged != null)
            {
                this.SelectedFrameChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("SelectedFrame");
        }

        public event EventHandler SelectedFrameChanged;

        private int _TileSize { get; set; }

        public int TileSize
        {
            get
            {
                return this._TileSize;
            }
            set
            {
                this._TileSize = value;
                this.OnTileSizeChanged();
            }
        }

        protected virtual void OnTileSizeChanged()
        {
            if (this.TileSizeChanged != null)
            {
                this.TileSizeChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("TileSize");
        }

        public event EventHandler TileSizeChanged;

        private LibraryBrowserImageMode _ImageMode { get; set; }

        public LibraryBrowserImageMode ImageMode
        {
            get
            {
                return this._ImageMode;
            }
            set
            {
                this._ImageMode = value;
                this.OnImageModeChanged();
            }
        }

        protected virtual void OnImageModeChanged()
        {
            if (this.ImageModeChanged != null)
            {
                this.ImageModeChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("ImageMode");
        }

        public event EventHandler ImageModeChanged;

        protected override async Task OnSignal(object sender, ISignal signal)
        {
            await base.OnSignal(sender, signal).ConfigureAwait(false);
            switch (signal.Name)
            {
                case CommonSignals.MetaDataUpdated:
                    await this.OnMetaDataUpdated(signal.State as MetaDataUpdatedSignalState).ConfigureAwait(false);
                    break;
                case CommonSignals.ImagesUpdated:
                    await this.Refresh().ConfigureAwait(false);
                    break;
            }
        }

        protected virtual Task OnMetaDataUpdated(MetaDataUpdatedSignalState state)
        {
            if (state != null)
            {
                return this.Refresh(state.FileDatas, state.Names, state.UpdateType);
            }
            else
            {
#if NET40
                return TaskEx.FromResult(false);
#else
                return Task.CompletedTask;
#endif
            }
        }

        protected virtual Task Refresh(IEnumerable<IFileData> fileDatas, IEnumerable<string> names, MetaDataUpdateType updateType)
        {
            if (updateType == MetaDataUpdateType.System)
            {
#if NET40
                return TaskEx.FromResult(false);
#else
                return Task.CompletedTask;
#endif
            }
            if (names != null && names.Any())
            {
                if (!names.Contains(CommonImageTypes.FrontCover, StringComparer.OrdinalIgnoreCase))
                {
#if NET40
                    return TaskEx.FromResult(false);
#else
                    return Task.CompletedTask;
#endif
                }
            }
            if (fileDatas != null && fileDatas.Any())
            {
#if NET40
                return TaskEx.FromResult(false);
#else
                return Task.CompletedTask;
#endif
            }
            return this.Refresh();
        }


        protected override async Task OnRefresh()
        {
            await base.OnRefresh().ConfigureAwait(false);
            await Windows.Invoke(() => this.Synchronize(new List<LibraryBrowserFrame>()
            {
                new LibraryBrowserFrame(LibraryHierarchyNode.Empty, this.Items)
            })).ConfigureAwait(false);
        }

        public bool IsPlaceholderSelected { get; private set; }

        public override LibraryHierarchyNode SelectedItem
        {
            get
            {
                if (this.IsPlaceholderSelected)
                {
                    return LibraryHierarchyNode.Empty;
                }
                return base.SelectedItem;
            }
            set
            {
                if (LibraryHierarchyNode.Empty.Equals(value))
                {
                    this.IsPlaceholderSelected = true;
                    return;
                }
                this.IsPlaceholderSelected = false;
                base.SelectedItem = value;
            }
        }

        protected override void OnSelectedItemChanged(object sender, EventArgs e)
        {
            this.IsPlaceholderSelected = false;
            var task = Windows.Invoke(this.Synchronize);
            base.OnSelectedItemChanged(sender, e);
        }

        public ICommand BrowseCommand
        {
            get
            {
                return CommandFactory.Instance.CreateCommand<bool>(this.Browse);
            }
        }

        public void Browse(bool up)
        {
            if (up)
            {
                this.Up();
                return;
            }
            if (this.AddToPlaylistCommand.CanExecute(false))
            {
                this.AddToPlaylistCommand.Execute(false);
                return;
            }
            if (this.IsPlaceholderSelected)
            {
                this.Up();
            }
            else
            {
                this.Down();
            }
        }

        private void Up()
        {
            var frame = this.Frames.LastOrDefault();
            this.Up(this.Frames);
            this.SelectedFrame = this.Frames.LastOrDefault();
            if (frame != null && !object.ReferenceEquals(this.SelectedItem, frame.ItemsSource))
            {
                this.SelectedItem = frame.ItemsSource;
            }
        }

        private void Up(IList<LibraryBrowserFrame> frames)
        {
            if (frames.Count <= 1)
            {
                return;
            }
            var frame = frames.LastOrDefault();
            if (frame == null)
            {
                return;
            }
            frames.Remove(frame);
        }

        private void Down()
        {
            var libraryHierarchyNode = this.SelectedItem;
            if (libraryHierarchyNode == null)
            {
                return;
            }
            this.Down(libraryHierarchyNode);
            this.SelectedFrame = this.Frames.LastOrDefault();
            if (this.SelectedFrame != null)
            {
                libraryHierarchyNode = this.SelectedFrame.Items.FirstOrDefault();
                if (!object.ReferenceEquals(this.SelectedItem, libraryHierarchyNode))
                {
                    this.SelectedItem = libraryHierarchyNode;
                }
            }
        }

        private void Down(LibraryHierarchyNode libraryHierarchyNode)
        {
            this.Down(libraryHierarchyNode, this.Frames);
        }

        private void Down(LibraryHierarchyNode libraryHierarchyNode, IList<LibraryBrowserFrame> frames)
        {
            var libraryHierarchyNodes = this.LibraryHierarchyBrowser.GetNodes(libraryHierarchyNode);
            if (!libraryHierarchyNodes.Any())
            {
                return;
            }
            var frame = new LibraryBrowserFrame(
                libraryHierarchyNode,
                libraryHierarchyNodes
            );
            frames.Add(frame);
        }

        private void Synchronize()
        {
            this.Synchronize(this.Frames);
        }

        private void Synchronize(IList<LibraryBrowserFrame> frames)
        {
            if (this.SelectedItem == null)
            {
                if (!object.ReferenceEquals(this.Frames, frames))
                {
                    this.Frames = new ObservableCollection<LibraryBrowserFrame>(frames);
                }
                return;
            }
            var path = new List<LibraryHierarchyNode>();
            var libraryHierarchyNode = this.SelectedItem;
            while (libraryHierarchyNode.Parent != null)
            {
                path.Insert(0, libraryHierarchyNode.Parent);
                libraryHierarchyNode = libraryHierarchyNode.Parent;
            }
            path.Insert(0, LibraryHierarchyNode.Empty);
            this.Synchronize(frames, path);
        }

        private void Synchronize(IList<LibraryBrowserFrame> frames, IList<LibraryHierarchyNode> path)
        {
            if (this.IsSynchronized(frames, path))
            {
                return;
            }
            for (var a = 0; a < path.Count; a++)
            {
                var libraryHierarchyNode = path[a];
                if (frames.Count > a)
                {
                    while (!object.ReferenceEquals(frames[a].ItemsSource, libraryHierarchyNode))
                    {
                        while (frames.Count > a)
                        {
                            this.Up(frames);
                        }
                        this.Down(libraryHierarchyNode, frames);
                    }
                }
                else
                {
                    this.Down(libraryHierarchyNode, frames);
                }
            }
            while (frames.Count > path.Count)
            {
                this.Up(frames);
            }
            if (!object.ReferenceEquals(this.Frames, frames))
            {
                this.Frames = new ObservableCollection<LibraryBrowserFrame>(frames);
            }
            this.SelectedFrame = this.Frames.LastOrDefault();
        }

        private bool IsSynchronized(IList<LibraryBrowserFrame> frames, IList<LibraryHierarchyNode> path)
        {
            if (!object.ReferenceEquals(this.Frames, frames))
            {
                return false;
            }
            if (frames.Count != path.Count)
            {
                return false;
            }
            for (var a = 0; a < path.Count; a++)
            {
                var frame = frames[a];
                var libraryHierarchyNode = path[a];
                if (!object.ReferenceEquals(frames[a].ItemsSource, libraryHierarchyNode))
                {
                    return false;
                }
                if (a > 0)
                {
                    //TODO: Can't quickly validate items, an extra item exists.
                }
                else
                {
                    if (!object.ReferenceEquals(frames[a].Items, this.Items))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        protected override Freezable CreateInstanceCore()
        {
            return new LibraryBrowser();
        }
    }
}
