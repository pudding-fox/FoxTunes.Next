using FoxDb.Interfaces;
using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;

namespace FoxTunes.ViewModel
{
    public class Genres : ViewModelBase
    {
        public IDatabaseFactory DatabaseFactory { get; private set; }

        private IEnumerable<string> _Names { get; set; }

        public IEnumerable<string> Names
        {
            get
            {
                return this._Names;
            }
            set
            {
                this._Names = value;
                this.OnNamesChanged();
            }
        }

        protected virtual void OnNamesChanged()
        {
            if (this.NamesChanged != null)
            {
                this.NamesChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("Names");
        }

        public event EventHandler NamesChanged;

        protected override void InitializeComponent(ICore core)
        {
            this.DatabaseFactory = core.Factories.Database;
            this.Dispatch(this.Refresh);
            base.InitializeComponent(core);
        }

        public async Task Refresh()
        {
            var names = await this.GetNames().ConfigureAwait(false);
            await Windows.Invoke(() => this.Names = names).ConfigureAwait(false);
        }

        protected virtual async Task<IEnumerable<string>> GetNames()
        {
            using (var database = this.DatabaseFactory.Create())
            {
                using (var transaction = database.BeginTransaction(database.PreferredIsolationLevel))
                {
                    using (var reader = database.ExecuteReader(database.Queries.GetLibraryMetaData, (parameters, phase) =>
                    {
                        switch (phase)
                        {
                            case DatabaseParameterPhase.Fetch:
                                parameters["name"] = CommonMetaData.Genre;
                                parameters["type"] = MetaDataItemType.Tag;
                                break;
                        }
                    }, transaction))
                    {
                        using (var sequence = reader.GetAsyncEnumerator())
                        {
                            var names = new List<string>();
                            while (await sequence.MoveNextAsync().ConfigureAwait(false))
                            {
                                names.Add(sequence.Current.Get<string>("value"));
                            }
                            return names;
                        }
                    }
                }
            }
        }

        protected override Freezable CreateInstanceCore()
        {
            return new Genres();
        }
    }
}
