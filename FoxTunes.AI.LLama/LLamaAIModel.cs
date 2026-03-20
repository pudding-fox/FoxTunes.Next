using System.IO;

namespace FoxTunes
{
    public static class LLamaAIModel
    {
        private static readonly string CurrentDirectory = Path.GetDirectoryName(typeof(LLamaAIModel).Assembly.Location);

        private static readonly string FileName = @"Models\biggie_groked_int8_q8_0.gguf";

        public static string ModelPath
        {
            get
            {
                return Path.Combine(CurrentDirectory, FileName);
            }
        }
    }
}
