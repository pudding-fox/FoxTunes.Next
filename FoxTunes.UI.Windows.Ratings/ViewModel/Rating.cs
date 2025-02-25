using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Shapes;

namespace FoxTunes.ViewModel
{
    public class Rating : ViewModelBase
    {
        static Rating()
        {
            Instances = new List<WeakReference<Rating>>();
            SignalEmitter = ComponentRegistry.Instance.GetComponent<ISignalEmitter>();
            if (SignalEmitter != null)
            {
                SignalEmitter.Signal += OnSignal;
            }
        }

        private static readonly IList<WeakReference<Rating>> Instances;

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
            if (state == null || state.Names == null || state.Names.Contains(CommonStatistics.Rating, StringComparer.OrdinalIgnoreCase))
            {
                foreach (var rating in Active)
                {
                    rating.Refresh();
                }
            }
        }

        public static IEnumerable<Rating> Active
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
            typeof(Rating),
            new PropertyMetadata(new PropertyChangedCallback(OnFileDataChanged))
        );

        public static IFileData GetFileData(Rating source)
        {
            return (IFileData)source.GetValue(FileDataProperty);
        }

        public static void SetFileData(Rating source, IFileData value)
        {
            source.SetValue(FileDataProperty, value);
        }

        public static void OnFileDataChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var rating = sender as Rating;
            if (rating == null)
            {
                return;
            }
            rating.OnFileDataChanged();
        }

        public Rating()
        {
            Instances.Add(new WeakReference<Rating>(this));
        }

        public bool Star1
        {
            get
            {
                return this.Value >= 1;
            }
            set
            {
                if (value || this.Value > 1)
                {
                    this.SetValue(1);
                }
                else
                {
                    this.SetValue(0);
                }
            }
        }

        public bool Star2
        {
            get
            {
                return this.Value >= 2;
            }
            set
            {
                if (value || this.Value > 1)
                {
                    this.SetValue(2);
                }
                else
                {
                    this.SetValue(1);
                }
            }
        }

        public bool Star3
        {
            get
            {
                return this.Value >= 3;
            }
            set
            {
                if (value || this.Value > 2)
                {
                    this.SetValue(3);
                }
                else
                {
                    this.SetValue(2);
                }
            }
        }

        public bool Star4
        {
            get
            {
                return this.Value >= 4;
            }
            set
            {
                if (value || this.Value > 3)
                {
                    this.SetValue(4);
                }
                else
                {
                    this.SetValue(3);
                }
            }
        }

        public bool Star5
        {
            get
            {
                return this.Value >= 5;
            }
            set
            {
                if (value || this.Value > 4)
                {
                    this.SetValue(5);
                }
                else
                {
                    this.SetValue(4);
                }
            }
        }

        protected virtual void OnValueChanged(byte value)
        {
            if (this.ValueChanged == null)
            {
                return;
            }
            this.ValueChanged(this, new RatingEventArgs(this.FileData, value));
        }

        public event RatingEventHandler ValueChanged;

        protected virtual byte GetValue()
        {
            var fileData = this.FileData;
            if (fileData == null)
            {
                return 0;
            }
            if (this.FileData != null)
            {
                lock (this.FileData.MetaDatas)
                {
                    return this.FileData.GetValueOrDefault<byte>(CommonStatistics.Rating, MetaDataItemType.Tag, default(byte));
                }
            }
            return 0;
        }

        protected virtual void SetValue(byte value)
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

        private byte Value { get; set; }

        public void Refresh()
        {
            var task = Windows.Invoke(() =>
            {
                var value = this.GetValue();
                if (this.Value == value)
                {
                    return;
                }
                this.Value = value;
                for (var a = 1; a <= 5; a++)
                {
                    this.OnPropertyChanged("Star" + a);
                }
            });
        }

        protected override Freezable CreateInstanceCore()
        {
            return new Rating();
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
