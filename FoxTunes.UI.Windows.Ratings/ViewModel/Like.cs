using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace FoxTunes.ViewModel
{
    public class Like : ViewModelBase
    {
        static Like()
        {
            Instances = new List<WeakReference<Like>>();
            SignalEmitter = ComponentRegistry.Instance.GetComponent<ISignalEmitter>();
            if (SignalEmitter != null)
            {
                SignalEmitter.Signal += OnSignal;
            }
        }

        private static readonly IList<WeakReference<Like>> Instances;

        private static readonly ISignalEmitter SignalEmitter;

        private static Task OnSignal(object sender, ISignal signal)
        {
            switch (signal.Name)
            {
                case CommonSignals.MetaDataUpdated:
                    OnMetaDataUpdated(signal.State as MetaDataUpdatedSignalState);
                    break;
            }
#if NET40
            return TaskEx.FromResult(false);
#else
            return Task.CompletedTask;
#endif
        }

        private static void OnMetaDataUpdated(MetaDataUpdatedSignalState state)
        {
            if (state == null || state.Names == null || state.Names.Contains(CommonStatistics.Like, StringComparer.OrdinalIgnoreCase))
            {
                foreach (var like in Active)
                {
                    like.Refresh();
                }
            }
        }

        public static IEnumerable<Like> Active
        {
            get
            {
                lock (Instances)
                {
                    return Instances
                        .Where(instance => instance != null && instance.IsAlive)
                        .Select(instance => instance.Target)
                        .ToArray();
                }
            }
        }

        public static readonly DependencyProperty FileDataProperty = DependencyProperty.Register(
            "FileData",
            typeof(IFileData),
            typeof(Like),
            new PropertyMetadata(new PropertyChangedCallback(OnFileDataChanged))
        );

        public static IFileData GetFileData(Like source)
        {
            return (IFileData)source.GetValue(FileDataProperty);
        }

        public static void SetFileData(Like source, IFileData value)
        {
            source.SetValue(FileDataProperty, value);
        }

        public static void OnFileDataChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var like = sender as Like;
            if (like == null)
            {
                return;
            }
            like.OnFileDataChanged();
        }

        public Like()
        {
            Instances.Add(new WeakReference<Like>(this));
        }

        protected virtual void OnValueChanged(bool value)
        {
            if (this.ValueChanged == null)
            {
                return;
            }
            this.ValueChanged(this, new LikeEventArgs(this.FileData, value));
        }

        public event LikeEventHandler ValueChanged;

        protected virtual bool GetValue()
        {
            var fileData = this.FileData;
            if (fileData == null)
            {
                return false;
            }
            if (this.FileData != null)
            {
                lock (this.FileData.MetaDatas)
                {
                    return this.FileData.GetValueOrDefault<bool>(CommonStatistics.Like, MetaDataItemType.Tag, default(bool));
                }
            }
            return false;
        }

        protected virtual void SetValue(bool value)
        {
            this.OnValueChanged(value);
        }

        public IFileData FileData
        {
            get
            {
                return GetFileData(this);
            }
            set
            {
                SetFileData(this, value);
            }
        }

        protected virtual void OnFileDataChanged()
        {
            this.Refresh();
            if (this.FileDataChanged != null)
            {
                this.FileDataChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("FileData");
        }

        public event EventHandler FileDataChanged;

        private bool _Value { get; set; }

        public bool Value
        {
            get
            {
                return this._Value;
            }
            set
            {
                this._Value = value;
                this.OnValueChanged();
            }
        }

        protected virtual void OnValueChanged()
        {
            this.SetValue(this.Value);
        }

        public void Refresh()
        {
            var task = Windows.Invoke(() =>
            {
                var value = this.GetValue();
                if (this.Value == value)
                {
                    return;
                }
                this._Value = value;
                this.OnPropertyChanged("Value");
            });
        }

        protected override Freezable CreateInstanceCore()
        {
            return new Like();
        }

        protected override void OnDisposing()
        {
            lock (Instances)
            {
                for (var a = Instances.Count - 1; a >= 0; a--)
                {
                    var instance = Instances[a];
                    if (instance == null || !instance.IsAlive)
                    {
                        Instances.RemoveAt(a);
                    }
                    else if (object.ReferenceEquals(this, instance.Target))
                    {
                        Instances.RemoveAt(a);
                    }
                }
            }
            base.OnDisposing();
        }
    }
}
