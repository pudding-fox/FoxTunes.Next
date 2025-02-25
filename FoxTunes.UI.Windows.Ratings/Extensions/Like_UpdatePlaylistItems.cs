using FoxTunes.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;

namespace FoxTunes
{
    public static partial class LikeExtensions
    {
        public static readonly IPlaylistManager PlaylistManager = ComponentRegistry.Instance.GetComponent<IPlaylistManager>();

        public static readonly LikeManager LikeManager = ComponentRegistry.Instance.GetComponent<LikeManager>();

        private static readonly ConditionalWeakTable<LikeBase, UpdatePlaylistItemsBehaviour> UpdatePlaylistItemsBehaviours = new ConditionalWeakTable<LikeBase, UpdatePlaylistItemsBehaviour>();

        public static readonly DependencyProperty UpdatePlaylistItemsProperty = DependencyProperty.RegisterAttached(
            "UpdatePlaylistItems",
            typeof(bool),
            typeof(LikeExtensions),
            new FrameworkPropertyMetadata(false, new PropertyChangedCallback(OnUpdatePlaylistItemsPropertyChanged))
        );

        public static bool GetUpdatePlaylistItems(LikeBase source)
        {
            return (bool)source.GetValue(UpdatePlaylistItemsProperty);
        }

        public static void SetUpdatePlaylistItems(LikeBase source, bool value)
        {
            source.SetValue(UpdatePlaylistItemsProperty, value);
        }

        private static void OnUpdatePlaylistItemsPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var like = sender as LikeBase;
            if (like == null)
            {
                return;
            }
            if (GetUpdatePlaylistItems(like))
            {
                var behaviour = default(UpdatePlaylistItemsBehaviour);
                if (!UpdatePlaylistItemsBehaviours.TryGetValue(like, out behaviour))
                {
                    UpdatePlaylistItemsBehaviours.Add(like, new UpdatePlaylistItemsBehaviour(like));
                }
            }
            else
            {
                var behaviour = default(UpdatePlaylistItemsBehaviour);
                if (UpdatePlaylistItemsBehaviours.TryGetValue(like, out behaviour))
                {
                    UpdatePlaylistItemsBehaviours.Remove(like);
                    behaviour.Dispose();
                }
            }
        }

        private class UpdatePlaylistItemsBehaviour : UIBehaviour<LikeBase>
        {
            public UpdatePlaylistItemsBehaviour(LikeBase like) : base(like)
            {
                this.Like = like;
                this.Like.ValueChanged += this.OnValueChanged;
            }

            public LikeBase Like { get; private set; }

            protected virtual void OnValueChanged(object sender, LikeEventArgs e)
            {
                var playlistItem = e.FileData as PlaylistItem;
                if (playlistItem == null)
                {
                    return;
                }
                var playlistItems = PlaylistManager.SelectedItems;
                if (playlistItems == null || !playlistItems.Contains(playlistItem))
                {
                    this.SetLike(new[] { playlistItem }, e.Value);
                }
                else
                {
                    this.SetLike(playlistItems.ToArray(), e.Value);
                }
            }

            protected virtual void SetLike(IEnumerable<PlaylistItem> playlistItems, bool like)
            {
                this.Dispatch(() => LikeManager.SetLike(playlistItems, like));
            }

            protected override void OnDisposing()
            {
                if (this.Like != null)
                {
                    this.Like.ValueChanged -= this.OnValueChanged;
                }
                base.OnDisposing();
            }
        }
    }
}
