using FoxTunes.Interfaces;
using System.IO;

namespace FoxTunes
{
    public abstract class BassEncoderTool : BassEncoderSettings, IBassEncoderTool
    {
        public abstract string Executable { get; }

        public virtual string Directory
        {
            get
            {
                return Path.GetDirectoryName(this.Executable);
            }
        }

        public abstract string GetArguments(EncoderItem encoderItem, IBassStream stream);
    }
}
