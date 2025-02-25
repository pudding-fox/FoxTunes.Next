using FoxTunes.Interfaces;
using MD.Net;

namespace FoxTunes
{
    public abstract class MinidiscTask : BackgroundTask
    {
        public const string ID = "7AE3FD56-9ADA-42F8-8A47-FE8BF5CDA54A";

        protected MinidiscTask() : base(ID)
        {

        }

        public IToolManager ToolManager { get; private set; }

        public IDeviceManager DeviceManager { get; private set; }

        public IFormatValidator FormatValidator { get; private set; }

        public IDiscManager DiscManager { get; private set; }

        public IFormatManager FormatManager { get; private set; }

        public IActionBuilder ActionBuilder { get; private set; }

        public override bool Visible
        {
            get
            {
                return true;
            }
        }

        public override void InitializeComponent(ICore core)
        {
            Logger.Write(this, LogLevel.Debug, "Initializing MD.Net framework..");
            this.ToolManager = new ToolManager();
            this.DeviceManager = new DeviceManager(this.ToolManager);
            this.FormatValidator = new FormatValidator();
            this.DiscManager = new DiscManager(this.ToolManager, this.FormatValidator);
            this.FormatManager = new FormatManager(this.ToolManager);
            this.ActionBuilder = new ActionBuilder(this.FormatManager);
            Logger.Write(this, LogLevel.Debug, "Initialized MD.Net framework.");
            base.InitializeComponent(core);
        }
    }
}
