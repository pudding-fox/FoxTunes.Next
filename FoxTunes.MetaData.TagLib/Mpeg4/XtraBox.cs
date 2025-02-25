using TagLib;
using TagLib.Mpeg4;

namespace FoxTunes.Mpeg4
{
    public class XtraBox : Box
    {
        public const short UNICODE = 8;

        public const short UINT64 = 19;

        public const short DATE = 21;

        public const short GUID = 72;

        public const short VARIANT = 72;

        public static readonly ReadOnlyByteVector Xtra = "Xtra";

        public XtraBox() : base(Xtra)
        {

        }

        private ByteVector _Data { get; set; }

        public override ByteVector Data
        {
            get
            {
                return this._Data;
            }
            set
            {
                this._Data = value;
            }
        }
    }
}
