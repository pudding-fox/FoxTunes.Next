using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;

namespace FoxTunes
{
    [ComponentPriority(ComponentPriorityAttribute.LOW)]
    [ComponentDependency(Slot = ComponentSlots.Output)]
    [ComponentDependency(Slot = ComponentSlots.UserInterface)]
    public class BassWasapiOutputDeviceSelector : BassOutputDeviceSelector
    {
        public override string Name
        {
            get
            {
                return Strings.WASAPI;
            }
        }

        public override bool IsActive
        {
            get
            {
                return string.Equals(this.OutputElement.Value.Id, BassWasapiStreamOutputConfiguration.OUTPUT_WASAPI_OPTION, StringComparison.OrdinalIgnoreCase);
            }
            set
            {
                if (!value)
                {
                    return;
                }
                var output = this.OutputElement.GetOption(BassWasapiStreamOutputConfiguration.OUTPUT_WASAPI_OPTION);
                if (output == null)
                {
                    return;
                }
                this.OutputElement.Value = output;
            }
        }

        public override IEnumerable<OutputDevice> Devices
        {
            get
            {
                foreach (var option in this.DeviceElement.Options)
                {
                    yield return new OutputDevice(this, option.Id, option.Name);
                }
            }
        }

        public override OutputDevice Device
        {
            get
            {
                return new OutputDevice(this, this.DeviceElement.Value.Id, this.DeviceElement.Value.Name);
            }
            set
            {
                if (value == null)
                {
                    return;
                }
                var device = this.DeviceElement.GetOption(value.Id);
                if (device == null)
                {
                    return;
                }
                this.DeviceElement.Value = device;
            }
        }

        public IConfiguration Configuration { get; private set; }

        public SelectionConfigurationElement OutputElement { get; private set; }

        public SelectionConfigurationElement DeviceElement { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.Configuration = core.Components.Configuration;
            this.OutputElement = this.Configuration.GetElement<SelectionConfigurationElement>(
                BassOutputConfiguration.SECTION,
                BassOutputConfiguration.OUTPUT_ELEMENT
            );
            this.DeviceElement = this.Configuration.GetElement<SelectionConfigurationElement>(
                BassOutputConfiguration.SECTION,
                BassWasapiStreamOutputConfiguration.ELEMENT_WASAPI_DEVICE
            );
            this.OutputElement.ValueChanged += this.OnValueChanged;
            this.DeviceElement.ValueChanged += this.OnValueChanged;
            base.InitializeComponent(core);
        }

        protected virtual void OnValueChanged(object sender, EventArgs e)
        {
            this.OnIsActiveChanged();
            this.OnDeviceChanged();
        }

        public override void Refresh()
        {
            this.DeviceElement.WithOptions(BassWasapiStreamOutputConfiguration.GetWASAPIDevices(), true);
            this.OnDevicesChanged();
        }

        protected override void OnDisposing()
        {
            if (this.OutputElement != null)
            {
                this.OutputElement.ValueChanged -= this.OnValueChanged;
            }
            if (this.DeviceElement != null)
            {
                this.DeviceElement.ValueChanged -= this.OnValueChanged;
            }
            base.OnDisposing();
        }
    }
}
