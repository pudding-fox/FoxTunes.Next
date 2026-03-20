using System.IO;

namespace FoxTunes
{
    public static class LLamaAIModel
    {
        private static readonly string CurrentDirectory = Path.GetDirectoryName(typeof(LLamaAIModel).Assembly.Location);

        private static readonly string FileName = @"Models\tinyllama-1.1b-chat-v1.0.Q2_K.gguf";

        public static string ModelPath
        {
            get
            {
                return Path.Combine(CurrentDirectory, FileName);
            }
        }
    }
}
