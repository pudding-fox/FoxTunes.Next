using NUnit.Framework;

namespace FoxTunes.Encoder.Bass.Tests
{
    [TestFixture]
    public class CoreTests
    {
        [SetUp]
        public void SetUp()
        {
            AssemblyResolver.Instance.EnableReflectionOnly();
        }

        [TearDown]
        public void TearDown()
        {
            AssemblyResolver.Instance.DisableReflectionOnly();
        }

        [Test]
        public void CanCreateEncoderCore()
        {
            var setup = new CoreSetup();
            setup.Disable(ComponentSlots.All);
            setup.Enable(ComponentSlots.Configuration);
            setup.Enable(ComponentSlots.Logger);
            using (var core = new Core(setup))
            {
                core.Load();
                core.Initialize();
            }
        }
    }
}
