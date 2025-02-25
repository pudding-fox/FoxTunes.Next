namespace FoxTunes.Interfaces
{
    public interface IBassEncoderWriter : IBaseComponent
    {
        void Write(byte[] data, int length);

        void Close();
    }
}
