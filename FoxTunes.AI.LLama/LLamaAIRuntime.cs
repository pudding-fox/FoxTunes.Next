using FoxTunes.Interfaces;
using LLama;
using LLama.Common;
using LLama.Native;
using LLama.Sampling;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace FoxTunes
{
    [Component(ID, ComponentSlots.AIRuntime)]
    public class LLamaAIRuntime : AIRuntime, IDisposable
    {
        const string ID = "782E9988-1260-48E1-BF1A-0995E34A9263";

        public static string VERSION = typeof(ChatSession).Assembly.GetName().Version.ToString();

        public LLamaAIRuntime() : base(ID, string.Format(Strings.LLamaAIRuntime_Name, VERSION))
        {
            this.ModelParams = new Lazy<ModelParams>(this.CreateModelParams);
            this.InferenceParams = new Lazy<InferenceParams>(this.CreateInferenceParams);
            this.Model = new Lazy<LLamaWeights>(this.CreateModel);
            this.Executor = new Lazy<InteractiveExecutor>(this.CreateExecutor);
            this.Embedder = new Lazy<LLamaEmbedder>(this.CreateEmbedder);
        }

        public Lazy<ModelParams> ModelParams { get; private set; }

        public Lazy<InferenceParams> InferenceParams { get; private set; }

        public Lazy<LLamaWeights> Model { get; private set; }

        public Lazy<InteractiveExecutor> Executor { get; private set; }

        public Lazy<LLamaEmbedder> Embedder { get; private set; }

        public ICore Core { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.Core = core;
            base.InitializeComponent(core);
        }

        protected virtual ModelParams CreateModelParams()
        {
            return new ModelParams(LLamaAIModel.ModelPath)
            {
                ContextSize = 8192,
                BatchSize = 4096,
                UBatchSize = 4096,
                Threads = 4,
                PoolingType = LLamaPoolingType.Mean,
                Embeddings = true
            };
        }

        protected virtual InferenceParams CreateInferenceParams()
        {
            return new InferenceParams()
            {
                MaxTokens = 256,
                AntiPrompts = new List<string> { "User:" },
                SamplingPipeline = new DefaultSamplingPipeline(),
            };
        }

        protected virtual LLamaWeights CreateModel()
        {
            return LLamaWeights.LoadFromFile(this.ModelParams.Value);
        }

        protected virtual InteractiveExecutor CreateExecutor()
        {
            return new InteractiveExecutor(this.Model.Value.CreateContext(this.ModelParams.Value));
        }

        protected virtual LLamaEmbedder CreateEmbedder()
        {
            return new LLamaEmbedder(this.Model.Value, this.ModelParams.Value);
        }

        public override ICorePrompts CorePrompts
        {
            get
            {
                var scripts = new LLamaAICorePrompts();
                scripts.InitializeComponent(this.Core);
                return scripts;
            }
        }

        public override IAIContext CreateContext(IEnumerable<IAIPrompt> prompts)
        {
            Logger.Write(this, LogLevel.Debug, "Creating AI context.");
            var history = new ChatHistory();
            foreach (var prompt in prompts)
            {
                switch (prompt.Type)
                {
                    case AIPromptType.Message:
                        history.AddMessage(AuthorRole.System, prompt.Prompt);
                        break;
                    case AIPromptType.Embedding:
                        foreach (var chunk in this.CreateEmbeddings(prompt.Prompt))
                        {
                            //TODO: Bad .Result
                            var embeddings = this.Embedder.Value.GetEmbeddings(chunk).Result;
                            var embedding = embeddings.Single();
                            history.AddMessage(AuthorRole.System, string.Join(", ", embedding));
                        }
                        break;
                }
            }
            var session = new ChatSession(this.Executor.Value, history);
            var context = new LLamaAIContext(session, this.InferenceParams.Value);
            context.InitializeComponent(this.Core);
            return context;
        }

        protected virtual IEnumerable<string> CreateEmbeddings(string prompt)
        {
            var builder = new StringBuilder();
            using (var reader = new StringReader(prompt))
            {
                var line = default(string);
                while ((line = reader.ReadLine()) != null)
                {
                    if (builder.Length + line.Length > this.ModelParams.Value.BatchSize)
                    {
                        yield return builder.ToString();
                        builder.Clear();
                    }
                    builder.AppendLine(line);
                }
            }
            if (builder.Length > 0)
            {
                yield return builder.ToString();
            }
        }

        public bool IsDisposed { get; private set; }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (this.IsDisposed || !disposing)
            {
                return;
            }
            this.OnDisposing();
            this.IsDisposed = true;
        }

        protected virtual void OnDisposing()
        {
            if (this.Embedder.IsValueCreated)
            {
                this.Embedder.Value.Dispose();
            }
        }

        ~LLamaAIRuntime()
        {
            Logger.Write(this, LogLevel.Error, "Component was not disposed: {0}", this.GetType().Name);
            try
            {
                this.Dispose(true);
            }
            catch
            {
                //Nothing can be done, never throw on GC thread.
            }
        }
    }
}
