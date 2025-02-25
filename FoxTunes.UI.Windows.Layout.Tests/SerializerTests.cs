using NUnit.Framework;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;

namespace FoxTunes
{
    [TestFixture]
    public class SerializerTests
    {
        [Test]
        public void SerializerTest001()
        {
            var xml = @"
<FoxTunes>
    <UIComponentConfiguration Component=""67A0F63C-DC86-4B4E-91E1-290B71822853"">
		<UIComponentConfiguration Component=""9AB8D410-B94D-492E-BF00-022A3E77762D""/>
	</UIComponentConfiguration>
</FoxTunes>";
            var stream = new MemoryStream(Encoding.Default.GetBytes(xml));
            var expected = new UIComponentConfiguration()
            {
                Component = new UIComponent("67A0F63C-DC86-4B4E-91E1-290B71822853"),
                Children = new ObservableCollection<UIComponentConfiguration>()
                {
                    new UIComponentConfiguration()
                    {
                        Component = new UIComponent("9AB8D410-B94D-492E-BF00-022A3E77762D")
                    }
                }
            };
            var actual = Serializer.LoadComponent(stream);
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void SerializerTest002()
        {
            var xml = @"
<FoxTunes>
    <UIComponentConfiguration Component=""A6820FDA-E415-40C6-AEFB-A73B6FBE4C93"">
        <UIComponentConfiguration Component=""67A0F63C-DC86-4B4E-91E1-290B71822853"">
		    <UIComponentConfiguration Component=""9AB8D410-B94D-492E-BF00-022A3E77762D""/>
	    </UIComponentConfiguration>
        <UIComponentConfiguration Component=""67A0F63C-DC86-4B4E-91E1-290B71822853"">
		    <UIComponentConfiguration Component=""9AB8D410-B94D-492E-BF00-022A3E77762D""/>
	    </UIComponentConfiguration>
    </UIComponentConfiguration>
</FoxTunes>";
            var stream = new MemoryStream(Encoding.Default.GetBytes(xml));
            var expected = new UIComponentConfiguration()
            {
                Component = new UIComponent("A6820FDA-E415-40C6-AEFB-A73B6FBE4C93"),
                Children = new ObservableCollection<UIComponentConfiguration>()
                {
                    new UIComponentConfiguration()
                    {
                        Component = new UIComponent("67A0F63C-DC86-4B4E-91E1-290B71822853"),
                        Children = new ObservableCollection<UIComponentConfiguration>()
                        {
                            new UIComponentConfiguration()
                            {
                                Component = new UIComponent("9AB8D410-B94D-492E-BF00-022A3E77762D")
                            }
                        }
                    },
                   new UIComponentConfiguration()
                    {
                        Component = new UIComponent("67A0F63C-DC86-4B4E-91E1-290B71822853"),
                        Children = new ObservableCollection<UIComponentConfiguration>()
                        {
                            new UIComponentConfiguration()
                            {
                                Component = new UIComponent("9AB8D410-B94D-492E-BF00-022A3E77762D")
                            }
                        }
                    }
                }
            };
            var actual = Serializer.LoadComponent(stream);
            Assert.AreEqual(expected, actual);
        }
    }
}
