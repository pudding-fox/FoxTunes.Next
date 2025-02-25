using System;
using System.Windows;

namespace FoxTunes
{
    [UIComponent("0F293AA4-E652-4DD4-A308-F5A28A5C1FAA", role: UIComponentRole.System)]
    public partial class LayoutSelector : UIComponentBase
    {
        public static readonly DependencyProperty IsEditableProperty = DependencyProperty.Register(
            "IsEditable",
            typeof(bool),
            typeof(LayoutSelector),
            new PropertyMetadata(true, new PropertyChangedCallback(OnIsEditableChanged))
        );

        public static bool GetIsEditable(LayoutSelector layoutSelector)
        {
            return (bool)layoutSelector.GetValue(IsEditableProperty);
        }

        public static void SetIsEditable(LayoutSelector layoutSelector, bool value)
        {
            layoutSelector.SetValue(IsEditableProperty, value);
        }

        public static void OnIsEditableChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var layoutSelector = sender as LayoutSelector;
            if (layoutSelector == null)
            {
                return;
            }
            layoutSelector.OnIsEditableChanged();
        }

        public LayoutSelector()
        {
            this.InitializeComponent();
        }

        public bool IsEditable
        {
            get
            {
                return GetIsEditable(this);
            }
            set
            {
                SetIsEditable(this, value);
            }
        }

        protected virtual void OnIsEditableChanged()
        {
            if (this.IsEditableChanged != null)
            {
                this.IsEditableChanged(this, EventArgs.Empty);
            }
        }

        public event EventHandler IsEditableChanged;
    }
}