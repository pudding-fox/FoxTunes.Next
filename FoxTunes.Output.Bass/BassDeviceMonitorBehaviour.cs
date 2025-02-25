using FoxDb;
using FoxTunes.Interfaces;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace FoxTunes
{
    public abstract class BassDeviceMonitorBehaviour : StandardBehaviour, IDisposable
    {
        public static readonly DataFlow[] HandledFlows = new[]
        {
            DataFlow.Render
        };

        public static readonly Role[] HandledRoles = new[]
        {
            Role.Console,
            Role.Multimedia
        };

        protected BassDeviceMonitorBehaviour(string id)
        {
            this.Id = id;
        }

        public string Id { get; private set; }

        public IBassOutput Output { get; private set; }

        public IOutputDeviceManager OutputDeviceManager { get; private set; }

        public IConfiguration Configuration { get; private set; }

        public BooleanConfigurationElement EnabledElement { get; private set; }

        public SelectionConfigurationElement OutputElement { get; private set; }

        public NotificationClient NotificationClient { get; private set; }

        new public bool IsInitialized { get; private set; }

        public bool IsEnabled
        {
            get
            {
                return this.EnabledElement.Value && string.Equals(this.OutputElement.Value.Id, this.Id, StringComparison.OrdinalIgnoreCase);
            }
        }

        public override void InitializeComponent(ICore core)
        {
            this.Output = core.Components.Output as IBassOutput;
            this.Output.Init += this.OnInit;
            this.Output.Free += this.OnFree;
            this.OutputDeviceManager = core.Managers.OutputDevice;
            this.Configuration = core.Components.Configuration;
            this.EnabledElement = this.Configuration.GetElement<BooleanConfigurationElement>(
                BassOutputConfiguration.SECTION,
                BassOutputConfiguration.DEVICE_MONITOR_ELEMENT
            );
            this.OutputElement = this.Configuration.GetElement<SelectionConfigurationElement>(
                BassOutputConfiguration.SECTION,
                BassOutputConfiguration.OUTPUT_ELEMENT
            );
            base.InitializeComponent(core);
        }

        protected virtual void OnInit(object sender, EventArgs e)
        {
            if (!this.IsEnabled || this.IsInitialized)
            {
                return;
            }
            this.NotificationClient = new NotificationClient();
            this.NotificationClient.DeviceAdded += this.OnDeviceAdded;
            this.NotificationClient.DeviceRemoved += this.OnDeviceRemoved;
            this.NotificationClient.DeviceStateChanged += this.OnDeviceStateChanged;
            this.NotificationClient.DefaultDeviceChanged += this.OnDefaultDeviceChanged;
            this.NotificationClient.PropertyValueChanged += this.OnPropertyValueChanged;
            this.IsInitialized = true;
            Logger.Write(this, LogLevel.Debug, "BASS Device Monitor Initialized.");
        }

        protected virtual void OnFree(object sender, EventArgs e)
        {
            if (!this.IsInitialized)
            {
                return;
            }
            if (this.NotificationClient != null)
            {
                this.NotificationClient.DeviceAdded -= this.OnDeviceAdded;
                this.NotificationClient.DeviceRemoved -= this.OnDeviceRemoved;
                this.NotificationClient.DeviceStateChanged -= this.OnDeviceStateChanged;
                this.NotificationClient.DefaultDeviceChanged -= this.OnDefaultDeviceChanged;
                this.NotificationClient.PropertyValueChanged -= this.OnPropertyValueChanged;
                this.NotificationClient.Dispose();
                this.NotificationClient = null;
            }
            this.IsInitialized = false;
        }

        protected virtual void OnDeviceAdded(object sender, NotificationClientEventArgs e)
        {
            //Nothing to do.
        }

        protected virtual void OnDeviceRemoved(object sender, NotificationClientEventArgs e)
        {
            //Nothing to do.
        }

        protected virtual void OnDeviceStateChanged(object sender, NotificationClientEventArgs e)
        {
            //Nothing to do.
        }

        protected virtual void OnDefaultDeviceChanged(object sender, NotificationClientEventArgs e)
        {
            if (!this.RestartRequired(e.Flow, e.Role, e.Device))
            {
                return;
            }
            Logger.Write(this, LogLevel.Debug, "The default playback device was changed: {0} => {1} => {2}", e.Flow.Value, e.Role.Value, e.Device);
            Logger.Write(this, LogLevel.Debug, "Restarting the output.");
            this.OutputDeviceManager.Restart();
        }

        protected virtual void OnPropertyValueChanged(object sender, NotificationClientEventArgs e)
        {
            //Nothing to do.
        }

        protected virtual bool RestartRequired(DataFlow? flow, Role? role, string device)
        {
            if (!this.Output.IsStarted)
            {
                return false;
            }
            if (!flow.HasValue || !HandledFlows.Contains(flow.Value))
            {
                return false;
            }
            if (!role.HasValue || !HandledRoles.Contains(role.Value))
            {
                return false;
            }
            return true;
        }

        public bool IsDisposed { get; private set; }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (this.IsDisposed || !disposing)
            {
                return;
            }
            this.OnDisposing();
            this.IsDisposed = true;
        }

        protected virtual void OnDisposing()
        {
            if (this.Output != null)
            {
                this.Output.Init -= this.OnInit;
                this.Output.Free -= this.OnFree;
            }
            this.OnFree(this, EventArgs.Empty);
        }

        ~BassDeviceMonitorBehaviour()
        {
            Logger.Write(this, LogLevel.Error, "Component was not disposed: {0}", this.GetType().Name);
            this.Dispose(true);
        }
    }
}