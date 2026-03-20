using FoxTunes.Interfaces;

namespace FoxTunes
{
    public class AIPrompt : BaseComponent, IAIPrompt
    {
        public AIPrompt(string prompt, AIPromptType type)
        {
            this.Prompt = prompt;
            this.Type = type;
        }

        public string Prompt { get; private set; }

        public AIPromptType Type { get; private set; }
    }
}
