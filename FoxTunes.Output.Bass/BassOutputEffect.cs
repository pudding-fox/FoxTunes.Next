using FoxTunes.Interfaces;

namespace FoxTunes
{
    public abstract class BassOutputEffect : OutputEffect
    {
        private bool _Available { get; set; }

        public override bool Available
        {
            get
            {
                return this._Available;
            }
            protected set
            {
                this._Available = value;
                this.OnAvailableChanged();
            }
        }

        public IConfiguration Configuration { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.Configuration = core.Components.Configuration;
            this.Configuration.GetElement<SelectionConfigurationElement>(
                BassOutputConfiguration.SECTION,
                BassOutputConfiguration.DEPTH_ELEMENT
            ).ConnectValue(value => this.Available = BassOutputConfiguration.GetFloat(value));
            base.InitializeComponent(core);
        }
    }
}
