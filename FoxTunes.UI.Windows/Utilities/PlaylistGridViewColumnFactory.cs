using FoxTunes.Interfaces;
using FoxTunes.ViewModel;
using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Linq;

namespace FoxTunes
{
    [WindowsUserInterfaceDependency]
    public class PlaylistGridViewColumnFactory : StandardComponent, IDisposable
    {
        static PlaylistGridViewColumnFactory()
        {
            AutoSizing = new List<PlaylistColumn>();
        }

        public static IList<PlaylistColumn> AutoSizing { get; private set; }

        public static bool IsAutoSizing(GridViewColumn gridViewColumn)
        {
            if (gridViewColumn is PlaylistGridViewColumn playlistGridViewColumn)
            {
                return AutoSizing.Contains(playlistGridViewColumn.PlaylistColumn);
            }
            return false;
        }

        public static void BeginAutoSize(GridViewColumn gridViewColumn)
        {
            if (gridViewColumn is PlaylistGridViewColumn playlistGridViewColumn)
            {
                AutoSizing.Add(playlistGridViewColumn.PlaylistColumn);
            }
        }

        public static void EndAutoSize(GridViewColumn gridViewColumn)
        {
            if (gridViewColumn is PlaylistGridViewColumn playlistGridViewColumn)
            {
                AutoSizing.Remove(playlistGridViewColumn.PlaylistColumn);
            }
        }

        public PlaylistColumnManager PlaylistColumnProviderManager { get; private set; }

        public IScriptingRuntime ScriptingRuntime { get; private set; }

        public Lazy<IScriptingContext> ScriptingContext { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.PlaylistColumnProviderManager = ComponentRegistry.Instance.GetComponent<PlaylistColumnManager>();
            this.ScriptingRuntime = core.Components.ScriptingRuntime;
            this.ScriptingContext = new Lazy<IScriptingContext>(this.ScriptingRuntime.CreateContext);
            base.InitializeComponent(core);
        }

        public PlaylistGridViewColumn Create(PlaylistColumn column)
        {
            var gridViewColumn = new PlaylistGridViewColumn(column);
            switch (column.Type)
            {
                case PlaylistColumnType.Script:
                    if (!string.IsNullOrEmpty(column.Script))
                    {
                        gridViewColumn.DisplayMemberBinding = new PlaylistScriptBinding()
                        {
                            ScriptingContext = this.ScriptingContext.Value,
                            Script = column.Script
                        };
                    }
                    break;
                case PlaylistColumnType.Plugin:
                    if (!string.IsNullOrEmpty(column.Plugin))
                    {
                        var provider = PlaylistColumnProviderManager.GetProvider(column.Plugin) as IUIPlaylistColumnProvider;
                        if (provider == null)
                        {
                            Logger.Write(this, LogLevel.Warn, "Playlist column plugin \"{0}\" was not found, has it been uninstalled?", column.Plugin);
                        }
                        else
                        {
                            gridViewColumn.CellTemplate = provider.CellTemplate;
                        }
                    }
                    break;
                case PlaylistColumnType.Tag:
                    if (!string.IsNullOrEmpty(column.Tag))
                    {
                        gridViewColumn.DisplayMemberBinding = new PlaylistMetaDataBinding()
                        {
                            Name = column.Tag,
                            Format = column.Format
                        };
                    }
                    break;
            }
            BindingHelper.Create(
                gridViewColumn,
                GridViewColumn.WidthProperty,
                typeof(GridViewColumn),
                column,
                "Width",
                ColumnWidthConverter.Instance,
                (sender, e) =>
                {
                    if (IsAutoSizing(gridViewColumn))
                    {
                        //Don't raise events while auto sizing is in progress.
                        return;
                    }
                    this.OnWidthChanged(column);
                }
            );
            BindingHelper.Create(
                gridViewColumn,
                GridViewColumnExtensions.PositionProperty,
                typeof(GridViewColumn),
                column,
                "Sequence",
                (sender, e) => this.OnPositionChanged(column)
            );
            return gridViewColumn;
        }

        protected virtual void OnWidthChanged(PlaylistColumn column)
        {
            if (this.WidthChanged == null)
            {
                return;
            }
            this.WidthChanged(this, column);
        }

        public event PlaylistColumnEventHandler WidthChanged;

        protected virtual void OnPositionChanged(PlaylistColumn column)
        {
            if (this.PositionChanged == null)
            {
                return;
            }
            this.PositionChanged(this, column);
        }

        public event PlaylistColumnEventHandler PositionChanged;

        public void Refresh(PlaylistGridViewColumn column)
        {
            column.Refresh();
        }

        public bool ShouldRefreshColumn(PlaylistGridViewColumn column, IEnumerable<string> names)
        {
            if (names == null || !names.Any())
            {
                return true;
            }
            switch (column.PlaylistColumn.Type)
            {
                case PlaylistColumnType.Script:
                    return names.Any(name => column.PlaylistColumn.Script.Contains(name, true));
                case PlaylistColumnType.Plugin:
                    var provider = PlaylistColumnProviderManager.GetProvider(column.PlaylistColumn.Plugin);
                    if (provider != null)
                    {
                        return provider.DependsOn(names);
                    }
                    break;
            }
            return true;
        }

        public bool IsDisposed { get; private set; }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (this.IsDisposed || !disposing)
            {
                return;
            }
            this.OnDisposing();
            this.IsDisposed = true;
        }

        protected virtual void OnDisposing()
        {
            if (this.ScriptingContext != null && this.ScriptingContext.IsValueCreated)
            {
                this.ScriptingContext.Value.Dispose();
            }
        }

        ~PlaylistGridViewColumnFactory()
        {
            Logger.Write(this.GetType(), LogLevel.Error, "Component was not disposed: {0}", this.GetType().Name);
            try
            {
                this.Dispose(true);
            }
            catch
            {
                //Nothing can be done, never throw on GC thread.
            }
        }
    }

    public delegate void PlaylistColumnEventHandler(object sender, PlaylistColumn playlistColumn);
}
