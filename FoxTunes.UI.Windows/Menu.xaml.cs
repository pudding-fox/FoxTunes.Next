using FoxTunes.Interfaces;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace FoxTunes
{
    /// <summary>
    /// Interaction logic for Menu.xaml
    /// </summary>
    public partial class Menu : ContextMenu
    {
        public Menu()
        {
            this.InitializeComponent();
        }

        public static readonly DependencyProperty CategoryProperty = DependencyProperty.Register(
            "Category",
            typeof(string),
            typeof(Menu)
        );

        public static string GetCategory(Menu source)
        {
            return (string)source.GetValue(CategoryProperty);
        }

        public static void SetCategory(Menu source, string value)
        {
            source.SetValue(CategoryProperty, value);
        }

        public string Category
        {
            get
            {
                return this.GetValue(CategoryProperty) as string;
            }
            set
            {
                this.SetValue(CategoryProperty, value);
            }
        }

        public static readonly DependencyProperty ComponentsProperty = DependencyProperty.Register(
            "Components",
            typeof(ObservableCollection<IInvocableComponent>),
            typeof(Menu)
        );

        public static ObservableCollection<IInvocableComponent> GetComponents(Menu source)
        {
            return (ObservableCollection<IInvocableComponent>)source.GetValue(ComponentsProperty);
        }

        public static void SetComponents(Menu source, ObservableCollection<IInvocableComponent> value)
        {
            source.SetValue(ComponentsProperty, value);
        }

        public ObservableCollection<IInvocableComponent> Components
        {
            get
            {
                return this.GetValue(ComponentsProperty) as ObservableCollection<IInvocableComponent>;
            }
            set
            {
                this.SetValue(ComponentsProperty, value);
            }
        }

        public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(
            "Source",
            typeof(object),
            typeof(Menu)
        );

        public static object GetSource(Menu source)
        {
            return source.GetValue(SourceProperty);
        }

        public static void SetSource(Menu source, object value)
        {
            source.SetValue(SourceProperty, value);
        }

        public object Source
        {
            get
            {
                return this.GetValue(SourceProperty);
            }
            set
            {
                this.SetValue(SourceProperty, value);
            }
        }

        public static readonly DependencyProperty ExplicitOrderingProperty = DependencyProperty.Register(
           "ExplicitOrdering",
           typeof(bool),
           typeof(Menu)
       );

        public static bool GetExplicitOrdering(Menu source)
        {
            return (bool)source.GetValue(ExplicitOrderingProperty);
        }

        public static void SetExplicitOrdering(Menu source, bool value)
        {
            source.SetValue(ExplicitOrderingProperty, value);
        }

        public bool ExplicitOrdering
        {
            get
            {
                return (bool)this.GetValue(ExplicitOrderingProperty);
            }
            set
            {
                this.SetValue(ExplicitOrderingProperty, value);
            }
        }
    }
}
