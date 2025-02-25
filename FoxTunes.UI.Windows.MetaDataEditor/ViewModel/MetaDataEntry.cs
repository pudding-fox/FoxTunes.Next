using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace FoxTunes.ViewModel
{
    public class MetaDataEntry : ViewModelBase
    {
        private static readonly string[] NUMERIC_TAGS = new[]
        {
            CommonMetaData.Disc,
            CommonMetaData.DiscCount,
            CommonMetaData.ReplayGainAlbumGain,
            CommonMetaData.ReplayGainAlbumPeak,
            CommonMetaData.ReplayGainTrackGain,
            CommonMetaData.ReplayGainTrackPeak,
            CommonMetaData.Track,
            CommonMetaData.TrackCount,
            CommonMetaData.Year
        };

        private static readonly string[] BOOLEAN_TAGS = new[]
        {
            CommonMetaData.IsCompilation
        };

        public static readonly string MyPictures = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);

        public static readonly ThemeLoader ThemeLoader = ComponentRegistry.Instance.GetComponent<ThemeLoader>();

        public static readonly ImageLoader ImageLoader = ComponentRegistry.Instance.GetComponent<ImageLoader>();

        public static readonly IFileSystemBrowser FileSystemBrowser = ComponentRegistry.Instance.GetComponent<IFileSystemBrowser>();

        private MetaDataEntry()
        {
            this.Sources = new List<IFileData>();
            this.MetaDataItems = new Dictionary<IFileData, MetaDataItem>();
        }

        public MetaDataEntry(string name, MetaDataItemType type) : this()
        {
            this.Name = name;
            this.Type = type;
        }

        private IList<IFileData> Sources { get; set; }

        private IDictionary<IFileData, MetaDataItem> MetaDataItems { get; set; }

        private string _Name { get; set; }

        public string Name
        {
            get
            {
                return this._Name;
            }
            set
            {
                this._Name = value;
                this.OnNameChanged();
            }
        }

        protected virtual void OnNameChanged()
        {
            this.OnIsNumericChanged();
            this.OnIsBooleanChanged();
            if (this.NameChanged != null)
            {
                this.NameChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("Name");
        }

        public event EventHandler NameChanged;

        private MetaDataItemType _Type { get; set; }

        public MetaDataItemType Type
        {
            get
            {
                return this._Type;
            }
            set
            {
                this._Type = value;
                this.OnTypeChanged();
            }
        }

        protected virtual void OnTypeChanged()
        {
            if (this.TypeChanged != null)
            {
                this.TypeChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("Type");
        }

        public event EventHandler TypeChanged;

        public string OriginalValue { get; protected set; }

        private string _Value { get; set; }

        public string Value
        {
            get
            {
                return this._Value;
            }
            set
            {
                this._Value = value;
                this.OnValueChanged();
            }
        }

        protected virtual void OnValueChanged()
        {
            if (string.Equals(this.Value, Strings.MetaDataEntry_NoValue, StringComparison.OrdinalIgnoreCase))
            {
                this.HasValue = false;
            }
            else
            {
                this.HasValue = true;
            }
            if (string.Equals(this.Value, Strings.MetaDataEntry_MultipleValues, StringComparison.OrdinalIgnoreCase))
            {
                this.HasMultipleValues = true;
            }
            else
            {
                this.HasMultipleValues = false;
            }
            this.OnHasChangesChanged();
            if (this.ValueChanged != null)
            {
                this.ValueChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("Value");
        }

        public event EventHandler ValueChanged;

        public bool IsNumeric
        {
            get
            {
                return NUMERIC_TAGS.Contains(this.Name, StringComparer.OrdinalIgnoreCase);
            }
        }

        protected virtual void OnIsNumericChanged()
        {
            if (this.IsNumericChanged != null)
            {
                this.IsNumericChanged(this, EventArgs.Empty);
            }
        }

        public event EventHandler IsNumericChanged;

        public bool IsBoolean
        {
            get
            {
                return BOOLEAN_TAGS.Contains(this.Name, StringComparer.OrdinalIgnoreCase);
            }
        }

        protected virtual void OnIsBooleanChanged()
        {
            if (this.IsBooleanChanged != null)
            {
                this.IsBooleanChanged(this, EventArgs.Empty);
            }
        }

        public event EventHandler IsBooleanChanged;

        public bool HasChanges
        {
            get
            {
                return !string.Equals(this.Value, this.OriginalValue);
            }
        }

        protected virtual void OnHasChangesChanged()
        {
            if (this.HasChangesChanged != null)
            {
                this.HasChangesChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("HasChanges");
        }

        public event EventHandler HasChangesChanged;

        private bool _HasValue { get; set; }

        public bool HasValue
        {
            get
            {
                return this._HasValue;
            }
            set
            {
                this._HasValue = value;
                this.OnHasValueChanged();
            }
        }

        protected virtual void OnHasValueChanged()
        {
            if (this.HasValueChanged != null)
            {
                this.HasValueChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("HasValue");
        }

        public event EventHandler HasValueChanged;

        private bool _HasMultipleValues { get; set; }

        public bool HasMultipleValues
        {
            get
            {
                return this._HasMultipleValues;
            }
            set
            {
                this._HasMultipleValues = value;
                this.OnHasMultipleValuesChanged();
            }
        }

        protected virtual void OnHasMultipleValuesChanged()
        {
            if (this.HasMultipleValuesChanged != null)
            {
                this.HasMultipleValuesChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("HasMultipleValues");
        }

        public event EventHandler HasMultipleValuesChanged;

        public void SetValue()
        {
            this.Value = Strings.MetaDataEntry_NoValue;
            foreach (var metaDataItem in this.MetaDataItems.Values)
            {
                if (string.IsNullOrEmpty(metaDataItem.Value))
                {
                    if (this.HasValue)
                    {
                        this.Value = Strings.MetaDataEntry_MultipleValues;
                        break;
                    }
                }
                else
                {
                    if (this.HasValue && !string.Equals(this.Value, metaDataItem.Value))
                    {
                        this.Value = Strings.MetaDataEntry_MultipleValues;
                        break;
                    }
                    else
                    {
                        this.Value = metaDataItem.Value;
                    }
                }
            }
            this.OriginalValue = this.Value;
        }

        public IEnumerable<IFileData> GetSources()
        {
            return this.Sources;
        }

        public void SetSources(IEnumerable<IFileData> sources)
        {
            foreach (var source in sources)
            {
                this.AddSource(source);
            }
        }

        protected virtual void AddSource(IFileData source)
        {
            this.Sources.Add(source);
            lock (source.MetaDatas)
            {
                var metaDataItem = source.MetaDatas.FirstOrDefault(
                    element => string.Equals(element.Name, this.Name, StringComparison.OrdinalIgnoreCase) && element.Type == this.Type
                );
                if (metaDataItem != null)
                {
                    this.MetaDataItems.Add(source, metaDataItem);
                }
            }
        }

        public IEnumerable<IFileData> Save()
        {
            if (!this.HasChanges || this.HasMultipleValues)
            {
                Enumerable.Empty<IFileData>();
            }
            var fileDatas = new List<IFileData>();
            foreach (var source in this.Sources)
            {
                if (this.HasValue)
                {
                    var metaDataItem = default(MetaDataItem);
                    if (!this.MetaDataItems.TryGetValue(source, out metaDataItem))
                    {
                        metaDataItem = new MetaDataItem(this.Name, this.Type);
                        lock (source.MetaDatas)
                        {
                            source.MetaDatas.Add(metaDataItem);
                        }
                        this.MetaDataItems.Add(source, metaDataItem);
                    }
                    if (!string.Equals(metaDataItem.Value, this.Value))
                    {
                        metaDataItem.Value = this.Value;
                        fileDatas.Add(source);
                    }
                }
                else
                {
                    var metaDataItem = default(MetaDataItem);
                    if (this.MetaDataItems.TryGetValue(source, out metaDataItem))
                    {
                        if (!string.IsNullOrEmpty(metaDataItem.Value))
                        {
                            metaDataItem.Value = null;
                            fileDatas.Add(source);
                        }
                    }
                }
            }
            this.OriginalValue = this.Value;
            return fileDatas;
        }

        public ICommand BrowseCommand
        {
            get
            {
                return new Command(this.Browse, () => this.CanBrowse);
            }
        }

        public bool CanBrowse
        {
            get
            {
                return this.Sources.Count > 0;
            }
        }

        public void Browse()
        {
            var directoryName = this.Sources.Select(
                playlistItem => playlistItem.DirectoryName
            ).FirstOrDefault();
            if (string.IsNullOrEmpty(directoryName))
            {
                directoryName = MyPictures;
            }
            var options = new BrowseOptions(
                "Select Artwork",
                directoryName,
                new[]
                {
                    new BrowseFilter("Images", ArtworkProvider.EXTENSIONS)
                },
                BrowseFlags.File
            );
            var result = FileSystemBrowser.Browse(options);
            if (!result.Success)
            {
                return;
            }
            this.Value = result.Paths.FirstOrDefault();
        }

        public ICommand DragEnterCommand
        {
            get
            {
                return new Command<DragEventArgs>(this.OnDragEnter);
            }
        }

        protected virtual void OnDragEnter(DragEventArgs e)
        {
            this.UpdateDragDropEffects(e);
        }

        public ICommand DragOverCommand
        {
            get
            {
                return new Command<DragEventArgs>(this.OnDragOver);
            }
        }

        protected virtual void OnDragOver(DragEventArgs e)
        {
            this.UpdateDragDropEffects(e);
        }

        protected virtual void UpdateDragDropEffects(DragEventArgs e)
        {
            var effects = DragDropEffects.None;
            try
            {
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    effects = DragDropEffects.Copy;
                }
            }
            catch (Exception exception)
            {
                Logger.Write(this, LogLevel.Warn, "Failed to query clipboard contents: {0}", exception.Message);
            }
            e.Effects = effects;
        }

        public ICommand DropCommand
        {
            get
            {
                return CommandFactory.Instance.CreateCommand<DragEventArgs>(
                    new Func<DragEventArgs, Task>(this.OnDrop)
                );
            }
        }

        protected virtual Task OnDrop(DragEventArgs e)
        {
            try
            {
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    var paths = e.Data.GetData(DataFormats.FileDrop) as IEnumerable<string>;
                    this.Value = paths.FirstOrDefault();
                }
            }
            catch (Exception exception)
            {
                Logger.Write(this, LogLevel.Warn, "Failed to process clipboard contents: {0}", exception.Message);
            }
#if NET40
            return TaskEx.FromResult(false);
#else
            return Task.CompletedTask;
#endif
        }

        public ICommand ClearCommand
        {
            get
            {
                return new Command(this.Clear, () => this.CanClear);
            }
        }

        public bool CanClear
        {
            get
            {
                return this.Sources.Count > 0 && this.HasValue;
            }
        }

        public void Clear()
        {
            this.Value = Strings.MetaDataEntry_NoValue;
        }

        protected override Freezable CreateInstanceCore()
        {
            return new MetaDataEntry();
        }
    }
}
