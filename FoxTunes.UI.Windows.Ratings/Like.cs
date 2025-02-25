using FoxTunes.Interfaces;
using System;
using System.Threading.Tasks;

namespace FoxTunes
{
    [UIComponent("4956029D-6075-4896-9CA3-75156203CFC3", role: UIComponentRole.Info)]
    [UIComponentToolbar(250, UIComponentToolbarAlignment.Left, true)]
    public class Like : LikeBase
    {
        public static IPlaybackManager PlaybackManager = ComponentRegistry.Instance.GetComponent<IPlaybackManager>();

        public static ILibraryBrowser LibraryBrowser = ComponentRegistry.Instance.GetComponent<ILibraryBrowser>();

        public static readonly LikeManager LikeManager = ComponentRegistry.Instance.GetComponent<LikeManager>();

        public static readonly BooleanConfigurationElement Popularimeter = ComponentRegistry.Instance.GetComponent<IConfiguration>().GetElement<BooleanConfigurationElement>(
            MetaDataBehaviourConfiguration.SECTION,
            MetaDataBehaviourConfiguration.READ_POPULARIMETER_TAGS
        );

        public Like()
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
                var viewModel = this.FindResource<global::FoxTunes.ViewModel.Like>("ViewModel");
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

        protected override void OnValueChanged(object sender, LikeEventArgs e)
        {
            if (e.FileData is LibraryItem libraryItem)
            {
                var task = LikeManager.SetLike(new[] { libraryItem }, e.Value);
            }
            else if (e.FileData is PlaylistItem playlistItem)
            {
                var task = LikeManager.SetLike(new[] { playlistItem }, e.Value);
            }
            base.OnValueChanged(sender, e);
        }
    }
}
