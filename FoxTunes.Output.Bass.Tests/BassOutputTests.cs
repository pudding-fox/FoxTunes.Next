using NUnit.Framework;
using System.Threading.Tasks;

namespace FoxTunes.Output.Bass.Tests
{
    [Explicit]
    [TestFixture(DEFAULT)]
    [TestFixture(DEFAULT | RESAMPLER)]
    [TestFixture(DEFAULT | ASIO)]
    [TestFixture(DEFAULT | RESAMPLER | ASIO)]
    [TestFixture(DEFAULT | RESAMPLER | WASAPI)]
    public class BassOutputTests : TestBase
    {
        public const long RESAMPLER = 128;

        public const long ASIO = 256;

        public const long WASAPI = 512;

        public static int RATE = 44100;

        public static bool FLOAT = true;

        public static int DS_DEVICE = -1;

        public static int ASIO_DEVICE = 0;

        public static int WASAPI_DEVICE = -1;

        public BassOutputTests(long configuration)
            : base(configuration)
        {

        }

        public override void SetUp()
        {
            base.SetUp();
            var output = this.Core.Components.Output as BassOutput;
            if (output == null)
            {
                Assert.Ignore("Requires \"{0}\".", typeof(BassOutput).Name);
            }
            var resampler = ComponentRegistry.Instance.GetComponent<BassResamplerStreamComponentBehaviour>();
            var ds = ComponentRegistry.Instance.GetComponent<BassDirectSoundStreamOutputBehaviour>();
            var asio = ComponentRegistry.Instance.GetComponent<BassAsioStreamOutputBehaviour>();
            var wasapi = ComponentRegistry.Instance.GetComponent<BassWasapiStreamOutputBehaviour>();
            if ((this.Configuration & RESAMPLER) != 0)
            {
                resampler.Enabled = true;
            }
            else
            {
                resampler.Enabled = false;
            }
            if ((this.Configuration & ASIO) != 0)
            {
                asio.Enabled = true;
                asio.AsioDevice = ASIO_DEVICE;
                ds.Enabled = false;
                wasapi.Enabled = false;
            }
            else if ((this.Configuration & WASAPI) != 0)
            {
                wasapi.Enabled = true;
                wasapi.WasapiDevice = WASAPI_DEVICE;
                ds.Enabled = false;
                asio.Enabled = false;
            }
            else
            {
                ds.Enabled = true;
                ds.DirectSoundDevice = DS_DEVICE;
                asio.Enabled = false;
                wasapi.Enabled = false;
            }
            output.Rate = RATE;
            output.Float = FLOAT;
        }

        [Test]
        public async Task CanPlayStream()
        {
            var outputStream = await this.Core.Components.Output.Load(TestInfo.PlaylistItems[0], false).ConfigureAwait(false);
            await outputStream.Play().ConfigureAwait(false);
            for (var a = 0; a <= 15; a++)
            {
#if NET40
                await TaskEx.Delay(1000).ConfigureAwait(false);
#else
                await Task.Delay(1000).ConfigureAwait(false);
#endif
                if (outputStream.Position == outputStream.Length)
                {
                    break;
                }
                else if (a == 15)
                {
                    Assert.Fail("Playback did not complete.");
                }
            }
        }

        [Test]
        public async Task CanPauseAndResumeStream()
        {
            var outputStream = await this.Core.Components.Output.Load(TestInfo.PlaylistItems[0], false).ConfigureAwait(false);
            await outputStream.Play().ConfigureAwait(false);
#if NET40
            await TaskEx.Delay(1000).ConfigureAwait(false);
#else
            await Task.Delay(1000).ConfigureAwait(false);
#endif
            Assert.IsTrue(outputStream.Position > 0);
            await outputStream.Pause().ConfigureAwait(false);
            var position = outputStream.Position;
#if NET40
            await TaskEx.Delay(1000).ConfigureAwait(false);
#else
            await Task.Delay(1000).ConfigureAwait(false);
#endif
            Assert.AreEqual(position, outputStream.Position);
            await outputStream.Resume().ConfigureAwait(false);
#if NET40
            await TaskEx.Delay(1000).ConfigureAwait(false);
#else
            await Task.Delay(1000).ConfigureAwait(false);
#endif
            Assert.IsTrue(outputStream.Position > position);
        }

        [Test]
        public async Task CanSeekStream()
        {
            var outputStream = await this.Core.Components.Output.Load(TestInfo.PlaylistItems[0], false).ConfigureAwait(false);
            var quarter = outputStream.Length / 4;
            var half = outputStream.Length / 2;
            await outputStream.Seek(quarter).ConfigureAwait(false);
            await outputStream.Play().ConfigureAwait(false);
#if NET40
            await TaskEx.Delay(1000).ConfigureAwait(false);
#else
            await Task.Delay(1000).ConfigureAwait(false);
#endif
            Assert.IsTrue(outputStream.Position > quarter);
            await outputStream.Seek(half).ConfigureAwait(false);
#if NET40
            await TaskEx.Delay(1000).ConfigureAwait(false);
#else
            await Task.Delay(1000).ConfigureAwait(false);
#endif
            Assert.IsTrue(outputStream.Position > half);
        }

        [Test]
        public async Task CanPerformGaplessTransition()
        {
            var outputStreams = new[]
            {
                await this.Core.Components.Output.Load(TestInfo.PlaylistItems[2], false).ConfigureAwait(false),
                await this.Core.Components.Output.Load(TestInfo.PlaylistItems[3], false).ConfigureAwait(false)
            };
            await outputStreams[0].Play().ConfigureAwait(false);
            await this.Core.Components.Output.Preempt(outputStreams[1]).ConfigureAwait(false);
            await outputStreams[0].Seek(outputStreams[0].Length - 1000).ConfigureAwait(false);
#if NET40
            await TaskEx.Delay(1000).ConfigureAwait(false);
#else
            await Task.Delay(1000).ConfigureAwait(false);
#endif
            Assert.AreEqual(outputStreams[0].Length, outputStreams[0].Position);
            Assert.IsTrue(outputStreams[1].Position > 0);
        }
    }
}
