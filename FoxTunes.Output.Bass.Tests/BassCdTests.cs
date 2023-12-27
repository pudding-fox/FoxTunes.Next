using NUnit.Framework;
using System.Threading.Tasks;

namespace FoxTunes.Output.Bass.Tests
{
    [Explicit]
    [TestFixture]
    public class BassCdTests : TestBase
    {
        [Test]
        public async Task CanOpenCd()
        {
            var behaviour = ComponentRegistry.Instance.GetComponent<BassCdBehaviour>();
            await behaviour.OpenCd(0).ConfigureAwait(false);
        }
    }
}
