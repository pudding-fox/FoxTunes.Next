using FoxTunes.Interfaces;
using LLama;
using LLama.Common;
using System;
using System.IO;
using System.Threading.Tasks;

namespace FoxTunes
{
    public class CreateAIPlaylistTask : PlaylistTaskBase
    {
        public CreateAIPlaylistTask(Playlist playlist, string prompt, int sequence = 0) : base(playlist, sequence)
        {
            this.Prompt = prompt;
        }

        public string Prompt { get; private set; }

        public TextConfigurationElement ModelLocation { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            base.InitializeComponent(core);
            this.ModelLocation = this.Configuration.GetElement<TextConfigurationElement>(
                AIPlaylistBehaviourConfiguration.SECTION,
                AIPlaylistBehaviourConfiguration.MODEL_LOCATION
            );
        }

        protected override Task OnStarted()
        {
            this.CreateModelParams();
            this.CreateModel();
            this.CreateContext();
            this.CreateExecutor();
            this.CreateHistory();
            return base.OnStarted();
        }

        protected override Task OnRun()
        {
            throw new NotImplementedException();
        }

        public ModelParams Parameters { get; private set; }

        public LLamaWeights Model { get; private set; }

        public LLamaContext Context { get; private set; }

        public InteractiveExecutor Executor { get; private set; }

        public ChatHistory History { get; private set; }

        public ChatSession Session { get; private set; }

        public InferenceParams InferenceParams { get; private set; }

        protected virtual void CreateModelParams()
        {
            if (!File.Exists(this.ModelLocation.Value))
            {
                throw new FileNotFoundException(string.Format("The expected model was not found: {0}", this.ModelLocation.Value));
            }
            this.Parameters = new ModelParams(this.ModelLocation.Value)
            {
                //TODO: Attributes.
            };
        }

        protected virtual void CreateModel()
        {
            this.Model = LLamaWeights.LoadFromFile(this.Parameters);
        }

        protected virtual void CreateContext()
        {
            this.Context = this.Model.CreateContext(this.Parameters);
        }

        protected virtual void CreateExecutor()
        {
            this.Executor = new InteractiveExecutor(this.Context);
        }

        protected virtual void CreateHistory()
        {
            this.History = new ChatHistory();
        }

        protected virtual void CreateSession()
        {
            this.Session = new ChatSession(this.Executor, this.History);
        }

        protected virtual void CreateInferenceParams()
        {
            this.InferenceParams = new InferenceParams()
            {
                //TODO: Attributes.
            };
        }
    }
}
