using NUnit.Framework;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FoxTunes.Encoder.Bass.Tests
{
    [Explicit]
    public class BassEncoderTests : TestBase
    {
        public string DirectoryName { get; private set; }

        [SetUp]
        public override void SetUp()
        {
            this.DirectoryName = Path.Combine(Path.GetTempPath(), string.Format("FT-{0}", DateTime.UtcNow.ToFileTimeUtc()));
            base.SetUp();
        }

        [TearDown]
        public override void TearDown()
        {
            if (Directory.Exists(this.DirectoryName))
            {
                Directory.Delete(this.DirectoryName, true);
            }
            base.TearDown();
        }

        [Test]
        public async Task CanEncodePlaylistItems()
        {
            var profiles = ComponentRegistry.Instance.GetComponents<IBassEncoderSettings>().Select(
                settings => settings.Name
            ).ToArray();
            var behaviour = ComponentRegistry.Instance.GetComponent<BassEncoderBehaviour>();
            foreach (var profile in profiles)
            {
                var encoderItems = await behaviour.Encode(
                    TestInfo.PlaylistItems,
                    behaviour.GetOutputPath(this.DirectoryName),
                    profile,
                    true
                ).ConfigureAwait(false);
                foreach (var encoderItem in encoderItems)
                {
                    Assert.AreEqual(EncoderItemStatus.Complete, encoderItem.Status, "Encode with profile \"{0}\" failed: {1}", profile, string.Join(", ", encoderItem.Errors));
                }
            }
        }
    }
}
