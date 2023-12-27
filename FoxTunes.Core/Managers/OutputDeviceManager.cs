using FoxDb;
using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FoxTunes
{
    [ComponentDependency(Slot = ComponentSlots.Output)]
    public class OutputDeviceManager : StandardManager, IOutputDeviceManager, IDisposable
    {
        public static readonly TimeSpan TIMEOUT = TimeSpan.FromMilliseconds(100);

        public OutputDeviceManager()
        {
            this.Debouncer = new AsyncDebouncer(TIMEOUT);
            this.Selectors = new List<IOutputDeviceSelector>();
        }

        public AsyncDebouncer Debouncer { get; private set; }

        public IList<IOutputDeviceSelector> Selectors { get; private set; }

        public IOutputDeviceSelector Selector
        {
            get
            {
                return this.Selectors.FirstOrDefault(selector => selector.IsActive);
            }
        }

        public IEnumerable<OutputDevice> Devices
        {
            get
            {
                return this.Selectors.SelectMany(selector => selector.Devices);
            }
        }

        protected virtual void OnDevicesChanged()
        {
            if (this.DevicesChanged != null)
            {
                this.DevicesChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("Devices");
        }

        public event EventHandler DevicesChanged;

        public OutputDevice Device
        {
            get
            {
                var selector = this.Selector;
                if (selector == null)
                {
                    return null;
                }
                return selector.Device;
            }
            set
            {
                if (value == null)
                {
                    return;
                }
                value.Selector.IsActive = true;
                value.Selector.Device = value;
            }
        }

        protected virtual void OnDeviceChanged()
        {
            if (this.DeviceChanged != null)
            {
                this.DeviceChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("Device");
        }

        public event EventHandler DeviceChanged;

        public IOutput Output { get; private set; }

        public IPlaylistManager PlaylistManager { get; private set; }

        public IPlaybackManager PlaybackManager { get; private set; }

        public IErrorEmitter ErrorEmitter { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.Output = core.Components.Output;
            this.PlaylistManager = core.Managers.Playlist;
            this.PlaybackManager = core.Managers.Playback;
            this.ErrorEmitter = core.Components.ErrorEmitter;
            this.Selectors.AddRange(
                ComponentRegistry.Instance.GetComponents<IOutputDeviceSelector>()
            );
            foreach (var selector in this.Selectors)
            {
                selector.IsActiveChanged += this.OnIsActiveChanged;
                selector.DevicesChanged += this.OnDevicesChanged;
                selector.DeviceChanged += this.OnDeviceChanged;
            }
            base.InitializeComponent(core);
        }

        protected virtual void OnIsActiveChanged(object sender, EventArgs e)
        {
            this.OnDeviceChanged();
        }

        protected virtual void OnDevicesChanged(object sender, EventArgs e)
        {
            if (this.IsRefreshing)
            {
                return;
            }
            this.OnDevicesChanged();
        }

        protected virtual void OnDeviceChanged(object sender, EventArgs e)
        {
            this.OnDeviceChanged();
        }

        public bool IsRefreshing { get; private set; }

        public void Refresh()
        {
            this.IsRefreshing = true;
            try
            {
                foreach (var selector in this.Selectors)
                {
                    selector.Refresh();
                }
            }
            finally
            {
                this.IsRefreshing = false;
                this.OnDevicesChanged();
            }
        }

        public void Restart()
        {
            if (!this.Output.IsStarted)
            {
                return;
            }
            this.Debouncer.Exec(this.OnRestart);
        }

        protected virtual async Task OnRestart()
        {
            var position = default(long);
            var paused = default(bool);
            var playlistItem = default(PlaylistItem);
            var outputStream = this.PlaybackManager.CurrentStream;
            if (outputStream != null)
            {
                position = outputStream.ActualPosition;
                paused = outputStream.IsPaused;
                playlistItem = outputStream.PlaylistItem;
            }
            try
            {
                await this.Output.Shutdown().ConfigureAwait(false);
                await this.Output.Start().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                await this.ErrorEmitter.Send(this, e).ConfigureAwait(false);
                return;
            }
            if (playlistItem != null)
            {
                try
                {
                    await this.PlaylistManager.Play(playlistItem).ConfigureAwait(false);
                    if (this.PlaybackManager.CurrentStream != null)
                    {
                        if (position > 0)
                        {
                            await this.PlaybackManager.CurrentStream.Seek(position).ConfigureAwait(false);
                        }
                        if (paused)
                        {
                            await this.PlaybackManager.CurrentStream.Pause().ConfigureAwait(false);
                        }
                    }
                }
                catch (Exception e)
                {
                    await this.ErrorEmitter.Send(this, e).ConfigureAwait(false);
                }
            }
        }

        protected override void OnDisposing()
        {
            if (this.Selectors != null)
            {
                foreach (var selector in this.Selectors)
                {
                    selector.IsActiveChanged -= this.OnIsActiveChanged;
                    selector.DevicesChanged -= this.OnDevicesChanged;
                    selector.DeviceChanged -= this.OnDeviceChanged;
                }
            }
            base.OnDisposing();
        }
    }
}
