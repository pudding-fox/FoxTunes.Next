using System;
using System.ComponentModel;
using System.Windows;

namespace FoxTunes
{
    public class FilteredEventTrigger : global::System.Windows.Interactivity.EventTrigger, INotifyPropertyChanged
    {
        public static readonly DependencyProperty SourceTypeProperty = DependencyProperty.Register(
           "SourceType",
           typeof(Type),
           typeof(FilteredEventTrigger),
           new PropertyMetadata(new PropertyChangedCallback(OnSourceTypeChanged))
       );

        public static Type GetSourceType(FilteredEventTrigger source)
        {
            return (Type)source.GetValue(SourceTypeProperty);
        }

        public static void SetSourceType(FilteredEventTrigger source, Type value)
        {
            source.SetValue(SourceTypeProperty, value);
        }

        public static void OnSourceTypeChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var filteredEventTrigger = sender as FilteredEventTrigger;
            if (filteredEventTrigger == null)
            {
                return;
            }
            filteredEventTrigger.OnSourceTypeChanged();
        }

        public Type SourceType
        {
            get
            {
                return this.GetValue(SourceTypeProperty) as Type;
            }
            set
            {
                this.SetValue(SourceTypeProperty, value);
            }
        }

        protected virtual void OnSourceTypeChanged()
        {
            if (this.SourceTypeChanged != null)
            {
                this.SourceTypeChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("SourceType");
        }

        public event EventHandler SourceTypeChanged;

        public static readonly DependencyProperty ExactMatchProperty = DependencyProperty.Register(
           "ExactMatch",
           typeof(bool),
           typeof(FilteredEventTrigger),
           new PropertyMetadata(false, new PropertyChangedCallback(OnExactMatchChanged))
       );

        public static bool GetExactMatch(FilteredEventTrigger source)
        {
            return (bool)source.GetValue(ExactMatchProperty);
        }

        public static void SetExactMatch(FilteredEventTrigger source, bool value)
        {
            source.SetValue(ExactMatchProperty, value);
        }

        public static void OnExactMatchChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var filteredEventTrigger = sender as FilteredEventTrigger;
            if (filteredEventTrigger == null)
            {
                return;
            }
            filteredEventTrigger.OnExactMatchChanged();
        }

        public bool ExactMatch
        {
            get
            {
                return (bool)this.GetValue(ExactMatchProperty);
            }
            set
            {
                this.SetValue(ExactMatchProperty, value);
            }
        }

        protected virtual void OnExactMatchChanged()
        {
            if (this.ExactMatchChanged != null)
            {
                this.ExactMatchChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("ExactMatch");
        }

        public event EventHandler ExactMatchChanged;

        protected override void OnEvent(EventArgs e)
        {
            if (this.SourceType != null)
            {
                var routedEventArgs = e as RoutedEventArgs;
                if (routedEventArgs != null)
                {
                    var dependencyObject = routedEventArgs.OriginalSource as DependencyObject;
                    if (dependencyObject != null)
                    {
                        if (this.ExactMatch)
                        {
                            if (!this.SourceType.IsAssignableFrom(dependencyObject.GetType()))
                            {
                                Console.WriteLine(dependencyObject.GetType().Name);
                                return;
                            }
                        }
                        else
                        {
                            var ancestor = dependencyObject.FindAncestor(this.SourceType);
                            if (ancestor == null)
                            {
                                return;
                            }
                        }
                    }
                }
            }
            base.OnEvent(e);
        }

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
