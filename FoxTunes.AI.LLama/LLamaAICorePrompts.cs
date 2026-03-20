using FoxDb.Interfaces;
using FoxTunes.Interfaces;
using System.Text;
using System.Threading.Tasks;

namespace FoxTunes.AI.LLama
{
    public class LLamaAICorePrompts : BaseComponent, ICorePrompts
    {
        public string AllTracks
        {
            get
            {
                //TODO: Bad .Result
                return string.Format(Strings.AllTracks, GetAllTracks().Result);
            }
        }

        public string CreatePlaylist(string prompt)
        {
            return string.Format(Strings.CreatePlaylist, prompt);
        }

        protected virtual async Task<string> GetAllTracks()
        {
            var builder = new StringBuilder();
            using (var database = this.DatabaseFactory.Create())
            {
                using (var transaction = database.BeginTransaction(database.PreferredIsolationLevel))
                {
                    using (var reader = this.GetReader(database, transaction))
                    {
                        using (var sequence = reader.GetAsyncEnumerator())
                        {
                            while (await sequence.MoveNextAsync().ConfigureAwait(false))
                            {
                                if (builder.Length > 0)
                                {
                                    builder.AppendLine();
                                }
                                builder.Append(string.Concat(
                                    "\"",
                                    sequence.Current.Get<string>("FileName"),
                                    "\", \"",
                                    sequence.Current.Get<string>("Name"),
                                    "\", \"",
                                    sequence.Current.Get<string>("Value"),
                                    "\""
                                ));
                            }
                        }
                    }
                }
            }
            return builder.ToString();
        }

        protected virtual IDatabaseReader GetReader(IDatabaseComponent database, ITransactionSource transaction)
        {
            return database.ExecuteReader(database.Queries.GetEntireLibrary, (parameters, phase) =>
            {
                switch (phase)
                {
                    case FoxDb.Interfaces.DatabaseParameterPhase.Fetch:
                        //Nothing to do.
                        break;
                }
            }, transaction);
        }

        public IDatabaseFactory DatabaseFactory { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.DatabaseFactory = core.Factories.Database;
            base.InitializeComponent(core);
        }
    }
}
