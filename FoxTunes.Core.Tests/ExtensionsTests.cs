using NUnit.Framework;
using System;

namespace FoxTunes
{
    [TestFixture]
    public class ExtensionsTests
    {
        [TestCase("Apple Juice", "Pear Juice", 0.6f)]

        [TestCase("Cat Fish", "cat fish", 1.0f)]
        public void Similarity_001(string subject, string value, float similarity)
        {
            Assert.AreEqual(Math.Round(similarity, 1), Math.Round(subject.Similarity(value, true), 1));
        }
    }
}
