using NUnit.Framework;
using System.Threading.Tasks;

namespace FoxTunes.Output.Bass.Tests
{
    [TestFixture]
    public class BassPipelineTests : TestBase
    {
        [Test]
        public async Task CanCreateStream()
        {
            await this.Core.Components.Output.Unload(
                await this.Core.Components.Output.Load(TestInfo.PlaylistItems[0], false)
.ConfigureAwait(false)).ConfigureAwait(false);
        }

        [Test]
        public async Task CannotPreemptDifferingStream()
        {
            var outputStreams = new[]
            {
                await this.Core.Components.Output.Load(TestInfo.PlaylistItems[0], false).ConfigureAwait(false),
                await this.Core.Components.Output.Load(TestInfo.PlaylistItems[1], false)
.ConfigureAwait(false)
            };
            await outputStreams[0].Play().ConfigureAwait(false);
            Assert.IsFalse(await this.Core.Components.Output.Preempt(outputStreams[1]).ConfigureAwait(false));
        }

        [Test]
        public async Task CanReconfigurePipeline()
        {
            var outputStreams = new[]
            {
                await this.Core.Components.Output.Load(TestInfo.PlaylistItems[0], false).ConfigureAwait(false),
                await this.Core.Components.Output.Load(TestInfo.PlaylistItems[1], false)
.ConfigureAwait(false)
            };
            await outputStreams[0].Play().ConfigureAwait(false);
            await outputStreams[1].Play().ConfigureAwait(false);
#if NET40
            await TaskEx.Delay(1000).ConfigureAwait(false);
#else
            await Task.Delay(1000).ConfigureAwait(false);
#endif
            Assert.IsTrue(outputStreams[1].Position > 0);
        }
    }
}
