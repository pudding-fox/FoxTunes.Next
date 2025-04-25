using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace FoxTunes
{
    [WindowsUserInterfaceDependency]
    //Requires Windows 11 22H2.
    [PlatformDependency(Major = 6, Minor = 2, Build = 22621)]
    public class WindowCoverArtAccentBehaviour : WindowBlurProvider
    {
        public const string ID = "CCCCD95E-98F0-4055-95EF-CC0393E8F1A7";

        public override string Id
        {
            get
            {
                return ID;
            }
        }

        public WindowCoverArtAccentBehaviour()
        {
            this.AccentColors = new Dictionary<IntPtr, Color>();
        }

        public IDictionary<IntPtr, Color> AccentColors { get; private set; }

        public IPlaybackManager PlaybackManager { get; private set; }

        public ILibraryBrowser LibraryBrowser { get; private set; }

        public IArtworkProvider ArtworkProvider { get; private set; }

        public ImageResizer ImageResizer { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.PlaybackManager = core.Managers.Playback;
            this.PlaybackManager.CurrentStreamChanged += this.OnCurrentStreamChanged;
            this.LibraryBrowser = core.Components.LibraryBrowser;
            this.ArtworkProvider = core.Components.ArtworkProvider;
            this.ImageResizer = ComponentRegistry.Instance.GetComponent<ImageResizer>();
            base.InitializeComponent(core);
        }

        private void OnCurrentStreamChanged(object sender, EventArgs e)
        {
            //Critical: Don't block in this event handler, it causes a deadlock.
            this.Dispatch(this.Refresh);
        }

        protected override async void OnRefresh()
        {
            var outputStream = this.PlaybackManager.CurrentStream;
            if (outputStream == null)
            {
                await this.OnRefresh(WindowExtensions.DefaultAccentColor).ConfigureAwait(false);
                return;
            }
            var fileData = default(IFileData);
            if (outputStream.PlaylistItem.LibraryItem_Id.HasValue)
            {
                fileData = this.LibraryBrowser.Get(outputStream.PlaylistItem.LibraryItem_Id.Value);
            }
            else
            {
                fileData = outputStream.PlaylistItem;
            }
            var fileName = await this.ArtworkProvider.Find(
                fileData,
                CommonImageTypes.FrontCover,
                ArtworkType.FrontCover
            ).ConfigureAwait(false);
            if (string.IsNullOrEmpty(fileName))
            {
                return;
            }
            var color = this.GetAccentColor(fileName);
            await this.OnRefresh(color).ConfigureAwait(false);
        }

        protected virtual async Task OnRefresh(Color color)
        {
            var windows = new HashSet<IntPtr>();
            foreach (var window in WindowBase.Active)
            {
                windows.Add(window.Handle);
                var currentColor = default(Color);
                if (this.AccentColors.TryGetValue(window.Handle, out currentColor) && !this.IsTransparencyEnabled)
                {
                    if (currentColor == color)
                    {
                        //Nothing to do.
                        continue;
                    }
                    await Windows.Invoke(() =>
                    {
                        ColorAnimation animation = new ColorAnimation(
                            currentColor,
                            color,
                            new Duration(TimeSpan.FromSeconds(1))
                        )
                        {
                            EasingFunction = new QuadraticEase()
                        };
                        window.BeginAnimation(WindowExtensions.AccentColorProperty, animation);
                    }).ConfigureAwait(false);
                }
                else
                {
                    await Windows.Invoke(() => WindowExtensions.SetAccentColor(window, color));
                }
                this.AccentColors[window.Handle] = color;
            }
            foreach (var handle in AccentColors.Keys.ToArray())
            {
                if (!windows.Contains(handle))
                {
                    AccentColors.Remove(handle);
                }
            }
        }

        protected virtual Color GetAccentColor(string fileName)
        {
            var color = this.ImageResizer.GetMainColor(fileName);
            return Color.FromArgb(
                WindowExtensions.DefaultAccentColor.A,
                color.R,
                color.G,
                color.B
            );
        }

        protected override void OnDisabled()
        {
            this.AccentColors.Clear();
            base.OnDisabled();
        }

        public override IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            return WindowCoverArtAccentBehaviourConfiguration.GetConfigurationSections();
        }

        protected override void OnDisposing()
        {
            if (this.PlaybackManager != null)
            {
                this.PlaybackManager.CurrentStreamChanged -= this.OnCurrentStreamChanged;
            }
            base.OnDisposing();
        }
    }
}
