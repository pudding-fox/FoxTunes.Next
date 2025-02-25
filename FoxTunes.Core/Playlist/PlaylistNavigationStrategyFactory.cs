using FoxTunes.Interfaces;
using System;

namespace FoxTunes
{
    //TODO: Not sure if/why this is required. 
    [ComponentPriority(ComponentPriorityAttribute.HIGH)]
    [ComponentDependency(Slot = ComponentSlots.Database)]
    public class PlaylistNavigationStrategyFactory : StandardComponent
    {
        public ICore Core { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.Core = core;
            base.InitializeComponent(core);
        }

        public PlaylistNavigationStrategy Create(string id)
        {
            var strategy = default(PlaylistNavigationStrategy);
            switch (id)
            {
                default:
                case PlaylistBehaviourConfiguration.ORDER_DEFAULT_OPTION:
                    strategy = new StandardPlaylistNavigationStrategy();
                    break;
                case PlaylistBehaviourConfiguration.ORDER_SHUFFLE_TRACKS:
                    strategy = new ShufflePlaylistNavigationStrategy();
                    break;
                case PlaylistBehaviourConfiguration.ORDER_SHUFFLE_ALBUMS:
                    strategy = new ShufflePlaylistNavigationStrategy(AlbumSelector);
                    break;
                case PlaylistBehaviourConfiguration.ORDER_SHUFFLE_ARTISTS:
                    strategy = new ShufflePlaylistNavigationStrategy(ArtistSelector);
                    break;
            }
            strategy.InitializeComponent(this.Core);
            return strategy;
        }

        private static string AlbumSelector(PlaylistItem playlistItem)
        {
            lock (playlistItem.MetaDatas)
            {
                foreach (var metaDataItem in playlistItem.MetaDatas)
                {
                    if (string.Equals(metaDataItem.Name, CommonMetaData.Album, StringComparison.OrdinalIgnoreCase))
                    {
                        return metaDataItem.Value;
                    }
                }
            }
            return null;
        }

        private static string ArtistSelector(PlaylistItem playlistItem)
        {
            lock (playlistItem.MetaDatas)
            {
                foreach (var metaDataItem in playlistItem.MetaDatas)
                {
                    if (string.Equals(metaDataItem.Name, CommonMetaData.Artist, StringComparison.OrdinalIgnoreCase))
                    {
                        return metaDataItem.Value;
                    }
                }
            }
            return null;
        }
    }
}
