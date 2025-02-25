using FoxTunes.Interfaces;
using System;
using System.Windows;

namespace FoxTunes.ViewModel
{
    public class GroupedPlaylist : DefaultPlaylist
    {
        protected override int MaxItems
        {
            get
            {
                return 1000;
            }
        }

        private string _GroupingScript { get; set; }

        public string GroupingScript
        {
            get
            {
                return this._GroupingScript;
            }
            set
            {
                this._GroupingScript = value;
                this.OnGroupingScriptChanged();
            }
        }

        protected virtual void OnGroupingScriptChanged()
        {
            if (this.GroupingScriptChanged != null)
            {
                this.GroupingScriptChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("GroupingScript");
        }

        public event EventHandler GroupingScriptChanged;

        public IConfiguration Configuration { get; private set; }

        protected override void InitializeComponent(ICore core)
        {
            base.InitializeComponent(core);
            this.Configuration = core.Components.Configuration;
            this.Configuration.GetElement<TextConfigurationElement>(
                PlaylistBehaviourConfiguration.SECTION,
                PlaylistGroupingBehaviourConfiguration.GROUP_SCRIPT_ELEMENT
            ).ConnectValue(value => this.GroupingScript = value);
        }

        protected override Freezable CreateInstanceCore()
        {
            return new GroupedPlaylist();
        }
    }
}
