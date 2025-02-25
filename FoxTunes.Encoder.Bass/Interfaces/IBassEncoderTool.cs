namespace FoxTunes.Interfaces
{
    public interface IBassEncoderTool : IBassEncoderSettings
    {
        string Executable { get; }

        string Directory { get; }

        string GetArguments(EncoderItem encoderItem, IBassStream stream);
    }
}
