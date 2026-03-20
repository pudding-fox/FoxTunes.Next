namespace FoxTunes.Interfaces
{
    public interface IAIPrompt : IBaseComponent
    {
        string Prompt { get; }

        AIPromptType Type { get; }
    }

    public enum AIPromptType : byte
    {
        None,
        Message,
        Embedding
    }
}
