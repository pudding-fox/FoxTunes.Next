using FoxTunes.Interfaces;
using System.IO;

namespace FoxTunes
{
    public class TagLibFileAbstraction : ITagLibFileAbstraction
    {
        public TagLibFileAbstraction(IFileAbstraction fileAbstraction)
        {
            this.FileAbstraction = fileAbstraction;
        }

        public IFileAbstraction FileAbstraction { get; private set; }

        public string Name
        {
            get
            {
                return this.FileAbstraction.FileName;
            }
        }

        public Stream ReadStream
        {
            get
            {
                return this.FileAbstraction.ReadStream;
            }
        }

        public Stream WriteStream
        {
            get
            {
                return this.FileAbstraction.WriteStream;
            }
        }

        public void CloseStream(Stream stream)
        {
            this.FileAbstraction.CloseStream(stream);
        }

        public static TagLibFileAbstraction Create(IFileAbstraction fileAbstraction)
        {
            return new TagLibFileAbstraction(fileAbstraction);
        }
    }
}
