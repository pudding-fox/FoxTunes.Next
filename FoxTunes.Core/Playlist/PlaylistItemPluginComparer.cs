using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;

namespace FoxTunes
{
    public class PlaylistItemPluginComparer : BaseComponent, IComparer<PlaylistItem>
    {
        public PlaylistItemPluginComparer(string plugin)
        {
            this.Plugin = plugin;
        }

        public string Plugin { get; private set; }

        public IPlaylistColumnProvider Provider { get; private set; }

        public int Changes { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.Provider = core.Managers.PlaylistColumn.GetProvider(this.Plugin);
            base.InitializeComponent(core);
        }

        public int Compare(PlaylistItem playlistItem1, PlaylistItem playlistItem2)
        {
            var value1 = this.GetValue(playlistItem1);
            var value2 = this.GetValue(playlistItem2);
            return this.Compare(value1, value2);
        }

        protected virtual int Compare(string value1, string value2)
        {
            var numeric1 = default(float);
            var numeric2 = default(float);
            var result = default(int);
            if (float.TryParse(value1, out numeric1) && float.TryParse(value2, out numeric2))
            {
                result = numeric1.CompareTo(numeric2);
            }
            else
            {
                result = string.Compare(value1, value2, StringComparison.OrdinalIgnoreCase);
            }
            if (result != 0)
            {
                this.Changes++;
            }
            return result;
        }

        protected virtual string GetValue(PlaylistItem playlistItem)
        {
            return this.Provider.GetValue(playlistItem);
        }
    }
}
