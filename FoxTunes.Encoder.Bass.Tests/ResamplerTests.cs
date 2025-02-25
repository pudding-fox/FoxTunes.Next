using FoxTunes.Interfaces;
using ManagedBass;
using NUnit.Framework;
using System;
using System.IO;
using System.Threading;

namespace FoxTunes.Encoder.Bass.Tests
{
    [TestFixture]
    [Explicit]
    public class ResamplerTests : TestBase
    {
        const int TIMEOUT = 60000; //One minute.

        public string DirectoryName { get; private set; }

        [SetUp]
        public override void SetUp()
        {
            global::ManagedBass.Bass.Init(global::ManagedBass.Bass.NoSoundDevice);
            this.DirectoryName = Path.Combine(Path.GetTempPath(), string.Format("FT-{0}", DateTime.UtcNow.ToFileTimeUtc()));
            base.SetUp();
        }

        [TearDown]
        public override void TearDown()
        {
            global::ManagedBass.Bass.Free();
            if (Directory.Exists(this.DirectoryName))
            {
                Directory.Delete(this.DirectoryName, true);
            }
            base.TearDown();
        }

        [TestCase(BassFlags.Default)]
        [TestCase(BassFlags.Float)]
        public void CanResample(BassFlags flags)
        {
            var encoderItemFactory = new EncoderItemFactory(new BassEncoderOutputPath.Fixed(this.DirectoryName));
            encoderItemFactory.InitializeComponent(this.Core);
            var profiles = ComponentRegistry.Instance.GetComponents<IBassEncoderSettings>();
            foreach (var profile in profiles)
            {
                foreach (var playlistItem in TestInfo.PlaylistItems)
                {
                    var encoderItem = encoderItemFactory.Create(new[] { playlistItem }, profile.Name)[0];
                    var streamFactory = ComponentRegistry.Instance.GetComponent<IBassStreamFactory>();
                    var stream = streamFactory.CreateBasicStream(playlistItem, flags);
                    using (var resampler = ResamplerFactory.Create(encoderItem, stream, profile))
                    {
                        var channelReader = new ChannelReader(encoderItem, stream);
                        var processWriter = new ProcessWriter(resampler.Process);
                        var processReader = new ProcessReader(resampler.Process);
                        var threads = new[]
                        {
                            new Thread(() => channelReader.CopyTo(processWriter, CancellationToken.None)),
                            new Thread(() => processReader.CopyTo((buffer, length) =>
                            {
                                //Nothing to do.
                            }, CancellationToken.None))
                        };
                        threads.ForEach(thread => thread.Start());
                        Assert.IsTrue(resampler.Process.WaitForExit(TIMEOUT));
                        threads.ForEach(thread => thread.Join());
                        Assert.AreEqual(0, resampler.Process.ExitCode, "Encode with profile \"{0}\" failed: Process does not indicate success.", profile.Name);
                    }
                    stream.Dispose();
                }
            }
        }
    }
}
