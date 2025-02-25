using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace FoxTunes.ViewModel
{
    public class ProfileSelector : ViewModelBase
    {
        public static readonly ProfilesBehaviour ProfilesBehaviour = ComponentRegistry.Instance.GetComponent<ProfilesBehaviour>();

        public IEnumerable<string> AvailableProfiles
        {
            get
            {
                return ProfilesBehaviour.AvailableProfiles;
            }
        }

        protected virtual void OnAvailableProfilesChanged()
        {
            if (this.AvailableProfilesChanged != null)
            {
                this.AvailableProfilesChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("AvailableProfiles");
        }

        public event EventHandler AvailableProfilesChanged;

        public string SelectedProfile
        {
            get
            {
                return ProfilesBehaviour.ActiveProfile;
            }
            set
            {
                ProfilesBehaviour.ActiveProfile = value;
            }
        }

        protected virtual void OnSelectedProfileChanged()
        {
            this.OnDeleteProfileCommandChanged();
            if (this.SelectedProfileChanged != null)
            {
                this.SelectedProfileChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("SelectedProfile");
        }

        public event EventHandler SelectedProfileChanged;

        protected override void InitializeComponent(ICore core)
        {
            ProfilesBehaviour.AvailableProfilesChanged += this.OnAvailableProfilesChanged;
            ProfilesBehaviour.ActiveProfileChanged += this.OnActiveProfileChanged;
            base.InitializeComponent(core);
        }

        protected virtual void OnAvailableProfilesChanged(object sender, EventArgs e)
        {
            var task = Windows.Invoke(this.OnAvailableProfilesChanged);
        }

        protected virtual void OnActiveProfileChanged(object sender, EventArgs e)
        {
            var task = Windows.Invoke(this.OnSelectedProfileChanged);
        }

        public ICommand AddProfileCommand
        {
            get
            {
                return CommandFactory.Instance.CreateCommand(this.AddProfile);
            }
        }

        public Task AddProfile()
        {
            return ProfilesBehaviour.New();
        }

        public ICommand DeleteProfileCommand
        {
            get
            {
                return CommandFactory.Instance.CreateCommand(this.DeleteProfile, () => this.CanDeleteProfile);
            }
        }

        protected virtual void OnDeleteProfileCommandChanged()
        {
            if (this.DeleteProfileCommandChanged != null)
            {
                this.DeleteProfileCommandChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("DeleteProfileCommand");
        }

        public event EventHandler DeleteProfileCommandChanged;

        public bool CanDeleteProfile
        {
            get
            {
                return !ProfilesBehaviour.IsDefaultProfile;
            }
        }

        public Task DeleteProfile()
        {
            return ProfilesBehaviour.Delete();
        }

        protected override Freezable CreateInstanceCore()
        {
            return new ProfileSelector();
        }

        protected override void OnDisposing()
        {
            if (ProfilesBehaviour != null)
            {
                ProfilesBehaviour.ActiveProfileChanged -= this.OnActiveProfileChanged;
                ProfilesBehaviour.ActiveProfileChanged -= this.OnActiveProfileChanged;
            }
            base.OnDisposing();
        }
    }
}
