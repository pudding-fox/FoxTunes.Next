using FoxTunes.Interfaces;
using SharpDX.Direct3D9;
using System;
using System.Collections.Generic;

namespace FoxTunes
{
    [WindowsUserInterfaceDependency]
    public class D3DRendererTargetBehaviour : RendererTargetBehaviour, IDisposable
    {
        public const string ID = "BBBBBE3C-1ED4-4CC6-9386-10D13823F63F";

        public override string Id
        {
            get
            {
                return ID;
            }
        }

        public IUserInterface UserInterface { get; private set; }

        public Direct3D Direct3D { get; private set; }

        public Device Device { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.UserInterface = core.Components.UserInterface;
            base.InitializeComponent(core);
        }

        public override RendererTarget Create(int width, int height)
        {
            this.EnsureDevice();
            return new D3DRendererTarget(this.Device, width, height);
        }

        protected virtual void EnsureDevice()
        {
            if (this.Device != null)
            {
                return;
            }
            //TODO: Bad .Result
            var window = this.UserInterface.GetMainWindow().Result;
            this.Direct3D = new Direct3D();
            this.Device = new Device(
                this.Direct3D,
                0,
                DeviceType.Hardware,
                window.Handle,
                CreateFlags.HardwareVertexProcessing,
                new PresentParameters()
                {
                    Windowed = true,
                    SwapEffect = SwapEffect.Discard,
                    DeviceWindowHandle = window.Handle,
                    PresentationInterval = PresentInterval.Immediate
                }
            );
        }

        public override IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            return D3DRendererTargetBehaviourConfiguration.GetConfigurationSections();
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
            if (this.Device != null)
            {
                this.Device.Dispose();
            }
            if (this.Direct3D != null)
            {
                this.Direct3D.Dispose();
            }
        }

        ~D3DRendererTargetBehaviour()
        {
            Logger.Write(this, LogLevel.Error, "Component was not disposed: {0}", this.GetType().Name);
            try
            {
                this.Dispose(true);
            }
            catch
            {
                //Nothing can be done, never throw on GC thread.
            }
        }
    }
}
