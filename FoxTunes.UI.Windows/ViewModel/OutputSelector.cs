using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;

namespace FoxTunes.ViewModel
{
    public class OutputSelector : ViewModelBase
    {
        public IEnumerable<OutputDevice> Devices
        {
            get
            {
                return this.OutputDeviceManager.Devices;
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
                return this.OutputDeviceManager.Device;
            }
            set
            {
                this.OutputDeviceManager.Device = value;
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

        public IOutputDeviceManager OutputDeviceManager { get; private set; }

        protected override void InitializeComponent(ICore core)
        {
            this.OutputDeviceManager = core.Managers.OutputDevice;
            this.OutputDeviceManager.DevicesChanged += this.OnDevicesChanged;
            this.OutputDeviceManager.DeviceChanged += this.OnDeviceChanged;
            base.InitializeComponent(core);
        }

        protected virtual void OnDevicesChanged(object sender, EventArgs e)
        {
            var task = Windows.Invoke(this.OnDevicesChanged);
        }

        protected virtual void OnDeviceChanged(object sender, EventArgs e)
        {
            var task = Windows.Invoke(this.OnDeviceChanged);
        }

        public ICommand RefreshCommand
        {
            get
            {
                return CommandFactory.Instance.CreateCommand(this.Refresh);
            }
        }

        public void Refresh()
        {
            this.OutputDeviceManager.Refresh();
        }

        public ICommand SettingsCommand
        {
            get
            {
                return CommandFactory.Instance.CreateCommand(this.Settings);
            }
        }

        public void Settings()
        {
            if (this.Device == null)
            {
                return;
            }
            var task = this.Device.Selector.ShowSettings();
        }

        protected override Freezable CreateInstanceCore()
        {
            return new OutputSelector();
        }

        protected override void OnDisposing()
        {
            if (this.OutputDeviceManager != null)
            {
                this.OutputDeviceManager.DevicesChanged -= this.OnDevicesChanged;
                this.OutputDeviceManager.DeviceChanged -= this.OnDeviceChanged;
            }
            base.OnDisposing();
        }
    }
}
