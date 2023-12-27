using NUnit.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FoxTunes.Output.Bass.ReplayGain.Tests
{
    [Explicit]
    public class BassReplayGainScannerTests : TestBase
    {
        public static PlaylistItem[] PlaylistItems
        {
            get
            {
                var position = 2;
                var playlistItems = new List<PlaylistItem>();
                foreach (var fileName in TestInfo.AudioFileNames)
                {
                    var album = string.Format("Album {0}", position / 2);
                    playlistItems.Add(new PlaylistItem()
                    {
                        FileName = fileName,
                        MetaDatas = new List<MetaDataItem>()
                        {
                            new MetaDataItem(CommonMetaData.Album, MetaDataItemType.Tag)
                            {
                                Value = album
                            }
                        }
                    });
                    position++;
                }
                return playlistItems.ToArray();
            }
        }

        [TestCase(ReplayGainMode.Track)]
        [TestCase(ReplayGainMode.Album)]
        public async Task CanScanPlaylistItems(ReplayGainMode mode)
        {
            var behaviour = ComponentRegistry.Instance.GetComponent<BassReplayGainScannerBehaviour>();
            await behaviour.Scan(PlaylistItems, mode).ConfigureAwait(false);
        }
    }
}
