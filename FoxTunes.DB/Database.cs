#pragma warning disable 612, 618
using FoxDb.Interfaces;
using FoxTunes.Interfaces;
using System.ComponentModel;
using System.Data;

namespace FoxTunes
{
    public abstract class Database : global::FoxDb.Database, IDatabaseComponent
    {
        protected static ILogger Logger
        {
            get
            {
                return LogManager.Logger;
            }
        }

        public Database(IProvider provider)
            : base(provider)
        {

        }

        public ICore Core { get; private set; }

        public abstract string FileName { get; }

        public abstract IsolationLevel PreferredIsolationLevel { get; }

        public IDatabaseTables Tables { get; private set; }

        public IDatabaseQueries Queries { get; private set; }

        public virtual void InitializeComponent(ICore core)
        {
            this.Core = core;
            this.Tables = new DatabaseTables(this);
            this.Tables.InitializeComponent(core);
            this.Queries = this.CreateQueries();
        }

        protected abstract IDatabaseQueries CreateQueries();

        protected virtual void OnPropertyChanging(string propertyName)
        {
            if (this.PropertyChanging == null)
            {
                return;
            }
            this.PropertyChanging(this, new PropertyChangingEventArgs(propertyName));
        }

        public event PropertyChangingEventHandler PropertyChanging;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            if (this.PropertyChanged == null)
            {
                return;
            }
            this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
