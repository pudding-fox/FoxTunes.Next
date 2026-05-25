using FoxTunes.Interfaces;

namespace FoxTunes
{
    public abstract class TestBase
    {
        protected TestBase()
        {
            var setup = new CoreSetup();
            setup.Disable(ComponentSlots.All);
            setup.Enable(ComponentSlots.Configuration);
            setup.Enable(ComponentSlots.Logger);
            this.Core = new Core(setup);
            this.Core.Load();
            this.Core.Initialize();
        }

        public ICore Core { get; private set; }
    }
}
