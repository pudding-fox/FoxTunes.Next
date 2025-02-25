using FoxTunes.Interfaces;
using System;
using System.Threading.Tasks;

namespace FoxTunes
{
    [UIComponent("CAF4D8AD-80C3-4421-90C1-3E063FA9D5CB", role: UIComponentRole.Info)]
    public class Rating : RatingBase
    {
        public static IPlaybackManager PlaybackManager = ComponentRegistry.Instance.GetComponent<IPlaybackManager>();

        public static ILibraryBrowser LibraryBrowser = ComponentRegistry.Instance.GetComponent<ILibraryBrowser>();

        public static readonly RatingManager RatingManager = ComponentRegistry.Instance.GetComponent<RatingManager>();

        public static readonly BooleanConfigurationElement Popularimeter = ComponentRegistry.Instance.GetComponent<IConfiguration>().GetElement<BooleanConfigurationElement>(
            MetaDataBehaviourConfiguration.SECTION,
            MetaDataBehaviourConfiguration.READ_POPULARIMETER_TAGS
        );

        public Rating()
        {
            //TODO: I can't work out how to make the star paths size to fit their parent.
            //TODO: This is a common size and will look OK in most cases.
            this.MaxHeight = 30;
            Popularimeter.ConnectValue(value =>
            {
                var task = Windows.Invoke(() =>
                {
                    if (value)
                    {
                        this.Enable();
                    }
                    else
                    {
                        this.Disable();
                    }
                });
            });
        }

        public void Enable()
        {
            this.IsComponentEnabled = true;
            PlaybackManager.CurrentStreamChanged += this.OnCurrentStreamChanged;
            var task = this.Refresh();
        }

        public void Disable()
        {
            this.IsComponentEnabled = false;
            PlaybackManager.CurrentStreamChanged -= this.OnCurrentStreamChanged;
        }

        protected virtual void OnCurrentStreamChanged(object sender, EventArgs e)
        {
            //Critical: Don't block in this event handler, it causes a deadlock.
            this.Dispatch(this.Refresh);
        }

        protected virtual Task Refresh()
        {
            return Windows.Invoke(() =>
            {
                var viewModel = this.FindResource<global::FoxTunes.ViewModel.Rating>("ViewModel");
                if (viewModel == null)
                {
                    return;
                }
                var outputStream = PlaybackManager.CurrentStream;
                if (outputStream != null)
                {
                    if (outputStream.PlaylistItem.LibraryItem_Id.HasValue)
                    {
                        viewModel.FileData = LibraryBrowser.Get(outputStream.PlaylistItem.LibraryItem_Id.Value);
                    }
                    else
                    {
                        viewModel.FileData = outputStream.PlaylistItem;
                    }
                }
                else
                {
                    viewModel.FileData = null;
                }
            });
        }

        protected override void OnValueChanged(object sender, RatingEventArgs e)
        {
            if (e.FileData is LibraryItem libraryItem)
            {
                var task = RatingManager.SetRating(new[] { libraryItem }, e.Value);
            }
            else if (e.FileData is PlaylistItem playlistItem)
            {
                var task = RatingManager.SetRating(new[] { playlistItem }, e.Value);
            }
            base.OnValueChanged(sender, e);
        }
    }
}
