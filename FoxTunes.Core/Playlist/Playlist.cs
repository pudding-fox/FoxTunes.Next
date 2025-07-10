using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FoxTunes
{
    public class Playlist : PersistableComponent, ISequenceableComponent
    {
        private int _Sequence { get; set; }

        public int Sequence
        {
            get
            {
                return this._Sequence;
            }
            set
            {
                if (this._Sequence == value)
                {
                    return;
                }
                this._Sequence = value;
                this.OnSequenceChanged();
            }
        }

        protected virtual void OnSequenceChanged()
        {
            if (this.SequenceChanged != null)
            {
                this.SequenceChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("Sequence");
        }

        public event EventHandler SequenceChanged;

        private string _Name { get; set; }

        public string Name
        {
            get
            {
                return this._Name;
            }
            set
            {
                if (string.Equals(this._Name, value))
                {
                    return;
                }
                this._Name = value;
                this.OnNameChanged();
            }
        }

        protected virtual void OnNameChanged()
        {
            if (this.NameChanged != null)
            {
                this.NameChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("Name");
        }

        public event EventHandler NameChanged;

        private PlaylistType _Type { get; set; }

        public PlaylistType Type
        {
            get
            {
                return this._Type;
            }
            set
            {
                if (this._Type == value)
                {
                    return;
                }
                this._Type = value;
                this.OnTypeChanged();
            }
        }

        protected virtual void OnTypeChanged()
        {
            if (this.TypeChanged != null)
            {
                this.TypeChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("Type");
        }

        public event EventHandler TypeChanged;

        private string _Config { get; set; }

        public string Config
        {
            get
            {
                return this._Config;
            }
            set
            {
                this._Config = value;
                this.OnConfigChanged();
            }
        }

        protected virtual void OnConfigChanged()
        {
            if (this.ConfigChanged != null)
            {
                this.ConfigChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("Config");
        }

        public event EventHandler ConfigChanged;

        private bool _Enabled { get; set; }

        public bool Enabled
        {
            get
            {
                return this._Enabled;
            }
            set
            {
                this._Enabled = value;
                this.OnEnabledChanged();
            }
        }

        protected virtual void OnEnabledChanged()
        {
            if (this.EnabledChanged != null)
            {
                this.EnabledChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("Enabled");
        }

        public event EventHandler EnabledChanged;

        public override int GetHashCode()
        {
            //We need a hash code for this type for performance reasons.
            //base.GetHashCode() returns 0.
            return this.Id.GetHashCode() * 29;
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj as Playlist);
        }

        public static string GetName(IEnumerable<Playlist> playlists)
        {
            var name = default(string);
            if (!playlists.Any())
            {
                name = "Default";
            }
            else
            {
                name = "New Playlist";
                for (var a = 1; a < 100; a++)
                {
                    var success = true;
                    foreach (var playlist in playlists)
                    {
                        if (string.Equals(playlist.Name, name, StringComparison.OrdinalIgnoreCase))
                        {
                            name = string.Format("New Playlist ({0})", a);
                            success = false;
                            break;
                        }
                    }
                    if (success)
                    {
                        return name;
                    }
                }
            }
            return name;
        }

        public static readonly Playlist Empty = new Playlist();
    }

    public enum PlaylistType : byte
    {
        None = 0,
        Selection = 1,
        Dynamic = 2,
        Smart = 3,
        Everything = 4,
        AI = 8
    }
}
