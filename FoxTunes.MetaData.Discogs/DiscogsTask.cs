using FoxTunes.Interfaces;

namespace FoxTunes
{
    public abstract class DiscogsTask : BackgroundTask
    {
        public const string ID = "926C84B8-2821-4462-BA4E-C1667C296847";

        protected DiscogsTask() : base(ID)
        {

        }

        public Discogs Discogs { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            var configuration = core.Components.Configuration;
            var baseUrl = configuration.GetElement<TextConfigurationElement>(
                DiscogsBehaviourConfiguration.SECTION,
                DiscogsBehaviourConfiguration.BASE_URL
            );
            var key = configuration.GetElement<TextConfigurationElement>(
                DiscogsBehaviourConfiguration.SECTION,
                DiscogsBehaviourConfiguration.CONSUMER_KEY
            );
            var secret = configuration.GetElement<TextConfigurationElement>(
                DiscogsBehaviourConfiguration.SECTION,
                DiscogsBehaviourConfiguration.CONSUMER_SECRET
            );
            var maxRequests = configuration.GetElement<IntegerConfigurationElement>(
                DiscogsBehaviourConfiguration.SECTION,
                DiscogsBehaviourConfiguration.MAX_REQUESTS
            );
            this.Discogs = new Discogs(baseUrl.Value, key.Value, secret.Value, maxRequests.Value);
            base.InitializeComponent(core);
        }

        protected override void OnDisposing()
        {
            if (this.Discogs != null)
            {
                this.Discogs.Dispose();
            }
            base.OnDisposing();
        }
    }
}
