using FoxTunes.Interfaces;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace FoxTunes
{
    public abstract class TestBase
    {
        public const long DEFAULT = 0;

        static TestBase()
        {
            AssemblyResolver.Instance.EnableReflectionOnly();
        }

        public static string Unique
        {
            get
            {
                return Guid.NewGuid().ToString("d").Split('-')[0];
            }
        }

        protected TestBase()
            : this(DEFAULT)
        {

        }

        protected TestBase(long configuration)
        {
            this.Configuration = configuration;
        }

        public ParallelOptions ParallelOptions
        {
            get
            {
                return new ParallelOptions()
                {
                    MaxDegreeOfParallelism = Environment.ProcessorCount
                };
            }
        }

        public long Configuration { get; private set; }

        public ICoreSetup Setup { get; private set; }

        public ICore Core { get; private set; }

        [SetUp]
        public virtual void SetUp()
        {
            ComponentResolver.Instance.Add(ComponentSlots.UserInterface, TestUserInterface.ID);
            this.Setup = CoreSetup.Default;
            this.Core = new Core(this.Setup);
            this.Core.Load();
            if (this.Core.Factories.Database.Test() != DatabaseTestResult.OK)
            {
                this.Core.Factories.Database.Initialize();
                using (var database = this.Core.Factories.Database.Create())
                {
                    this.Core.InitializeDatabase(database, DatabaseInitializeType.All);
                }
            }
            this.Core.Initialize();
        }

        [TearDown]
        public virtual void TearDown()
        {
            ComponentResolver.Instance.Remove(ComponentSlots.Database);
            this.Core.Dispose();
            this.Core = null;
        }
    }
}
