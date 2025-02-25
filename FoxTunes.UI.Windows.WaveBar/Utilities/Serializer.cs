using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace FoxTunes
{
    public class Serializer
    {
        public Serializer()
        {
            this.Formatter = new BinaryFormatter();
            this.Formatter.Binder = new Binder();
        }

        public BinaryFormatter Formatter { get; private set; }

        public object Read(Stream stream)
        {
            return this.Formatter.Deserialize(stream);
        }

        public void Write(Stream stream, object value)
        {
            this.Formatter.Serialize(stream, value);
        }

        public static readonly Serializer Instance = new Serializer();

        private class Binder : SerializationBinder
        {
            public override Type BindToType(string assemblyName, string typeName)
            {
                //We only deal with types defined in this assembly.
                return this.GetType().Assembly.GetType(typeName);
            }
        }
    }
}
