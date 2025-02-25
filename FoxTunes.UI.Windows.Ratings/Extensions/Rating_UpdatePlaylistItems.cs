using FoxTunes.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;

namespace FoxTunes
{
    public static partial class RatingExtensions
    {
        public static readonly IPlaylistManager PlaylistManager = ComponentRegistry.Instance.GetComponent<IPlaylistManager>();

        public static readonly RatingManager RatingManager = ComponentRegistry.Instance.GetComponent<RatingManager>();

        private static readonly ConditionalWeakTable<RatingBase, UpdatePlaylistItemsBehaviour> UpdatePlaylistItemsBehaviours = new ConditionalWeakTable<RatingBase, UpdatePlaylistItemsBehaviour>();

        public static readonly DependencyProperty UpdatePlaylistItemsProperty = DependencyProperty.RegisterAttached(
            "UpdatePlaylistItems",
            typeof(bool),
            typeof(RatingExtensions),
            new FrameworkPropertyMetadata(false, new PropertyChangedCallback(OnUpdatePlaylistItemsPropertyChanged))
        );

        public static bool GetUpdatePlaylistItems(RatingBase source)
        {
            return (bool)source.GetValue(UpdatePlaylistItemsProperty);
        }

        public static void SetUpdatePlaylistItems(RatingBase source, bool value)
        {
            source.SetValue(UpdatePlaylistItemsProperty, value);
        }

        private static void OnUpdatePlaylistItemsPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var rating = sender as RatingBase;
            if (rating == null)
            {
                return;
            }
            if (GetUpdatePlaylistItems(rating))
            {
                var behaviour = default(UpdatePlaylistItemsBehaviour);
                if (!UpdatePlaylistItemsBehaviours.TryGetValue(rating, out behaviour))
                {
                    UpdatePlaylistItemsBehaviours.Add(rating, new UpdatePlaylistItemsBehaviour(rating));
                }
            }
            else
            {
                var behaviour = default(UpdatePlaylistItemsBehaviour);
                if (UpdatePlaylistItemsBehaviours.TryGetValue(rating, out behaviour))
                {
                    UpdatePlaylistItemsBehaviours.Remove(rating);
                    behaviour.Dispose();
                }
            }
        }

        private class UpdatePlaylistItemsBehaviour : UIBehaviour<RatingBase>
        {
            public UpdatePlaylistItemsBehaviour(RatingBase rating) : base(rating)
            {
                this.Rating = rating;
                this.Rating.ValueChanged += this.OnValueChanged;
            }

            public RatingBase Rating { get; private set; }

            protected virtual void OnValueChanged(object sender, RatingEventArgs e)
            {
                var playlistItem = e.FileData as PlaylistItem;
                if (playlistItem == null)
                {
                    return;
                }
                var playlistItems = PlaylistManager.SelectedItems;
                if (playlistItems == null || !playlistItems.Contains(playlistItem))
                {
                    this.SetRating(new[] { playlistItem }, e.Value);
                }
                else
                {
                    this.SetRating(playlistItems.ToArray(), e.Value);
                }
            }

            protected virtual void SetRating(IEnumerable<PlaylistItem> playlistItems, byte rating)
            {
                this.Dispatch(() => RatingManager.SetRating(playlistItems, rating));
            }

            protected override void OnDisposing()
            {
                if (this.Rating != null)
                {
                    this.Rating.ValueChanged -= this.OnValueChanged;
                }
                base.OnDisposing();
            }
        }
    }
}
