using FoxTunes.Interfaces;
using System.Collections.Generic;
using System.Linq;

namespace FoxTunes
{
    public static class CommonSignals
    {
        public const string LibraryUpdated = "LibraryUpdated";

        public const string HierarchiesUpdated = "HierarchiesUpdated";

        public const string PlaylistUpdated = "PlaylistUpdated";

        public const string PlaylistSelected = "PlaylistSelected";

        public const string PlaylistColumnsUpdated = "PlaylistColumnsUpdated";

        public const string MetaDataUpdated = "MetaDataUpdated";

        public const string MetaDataProvidersUpdated = "MetaDataProvidersUpdated";

        public const string SettingsUpdated = "SettingsUpdated";

        public const string ImagesUpdated = "ImagesUpdated";

        public const string PluginInvocation = "PluginInvocation";
    }

    public class PlaylistUpdatedSignalState : SignalState
    {
        public PlaylistUpdatedSignalState()
        {
            this.Playlists = new Playlist[] { };
        }

        public PlaylistUpdatedSignalState(Playlist playlist, DataSignalType type) : this()
        {
            if (playlist != null)
            {
                this.Playlists = new[] { playlist };
            }
            this.Type = type;
        }

        public PlaylistUpdatedSignalState(IEnumerable<Playlist> playlists, DataSignalType type) : this()
        {
            if (playlists != null)
            {
                this.Playlists = playlists.ToArray();
            }
            this.Type = type;
        }

        public Playlist[] Playlists { get; private set; }

        public DataSignalType Type { get; private set; }
    }

    public class MetaDataUpdatedSignalState : SignalState
    {
        public MetaDataUpdatedSignalState()
        {
            this.FileDatas = new IFileData[] { };
            this.Names = new string[] { };
        }

        public MetaDataUpdatedSignalState(IEnumerable<IFileData> fileDatas, IEnumerable<string> names, MetaDataUpdateType updateType) : this()
        {
            if (fileDatas != null)
            {
                this.FileDatas = fileDatas.ToArray();
            }
            if (names != null)
            {
                this.Names = names.ToArray();
            }
            this.UpdateType = updateType;
        }

        public IFileData[] FileDatas { get; private set; }

        public string[] Names { get; private set; }

        public MetaDataUpdateType UpdateType { get; private set; }
    }

    public class PluginInvocationSignalState : SignalState
    {
        public PluginInvocationSignalState(string id)
        {
            this.Id = id;
        }

        public string Id { get; private set; }
    }

    public class PlaylistColumnsUpdatedSignalState : SignalState
    {
        public PlaylistColumnsUpdatedSignalState(IEnumerable<PlaylistColumn> columns, DataSignalType type)
        {
            this.Columns = columns;
            this.Type = type;
        }

        public IEnumerable<PlaylistColumn> Columns { get; private set; }

        public DataSignalType Type { get; private set; }
    }

    public enum DataSignalType : byte
    {
        None,
        Added,
        Updated,
        Removed
    }
}
