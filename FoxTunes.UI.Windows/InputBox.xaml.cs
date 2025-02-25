using FoxTunes.Interfaces;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace FoxTunes
{
    /// <summary>
    /// Interaction logic for InputBox.xaml
    /// </summary>
    public partial class InputBox : WindowBase
    {
        public const string ID = "1EC1A063-DC6A-4264-8882-0A01B8F6B80E";

        public InputBox()
        {
            this.InitializeComponent();
        }

        public override string Id
        {
            get
            {
                return ID;
            }
        }

        protected virtual void OnLoaded(object sender, RoutedEventArgs e)
        {
            var children = this.Result.FindChildren<Control>();
            foreach (var child in children)
            {
                child.Focus();
            }
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                this.DialogResult = true;
            }
            else if (e.Key == Key.Escape)
            {
                this.DialogResult = false;
            }
            base.OnPreviewKeyDown(e);
        }

        protected virtual void OnOKClick(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        protected virtual void OnCancelClick(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        public static string ShowDialog(string prompt, string value, UserInterfacePromptFlags flags = UserInterfacePromptFlags.None)
        {
            var registration = default(Windows.WindowRegistration);
            if (Windows.Registrations.TryGet(ID, out registration))
            {
                var instance = registration.GetInstance();
                var viewModel = instance.TryFindResource("ViewModel") as global::FoxTunes.ViewModel.InputBox;
                if (viewModel != null)
                {
                    viewModel.Prompt = prompt;
                    viewModel.Flags = flags;
                    if (!string.IsNullOrEmpty(value) && viewModel.Result != null)
                    {
                        viewModel.Result.SetResult(value);
                    }
                    if (instance.ShowDialog().GetValueOrDefault())
                    {
                        return viewModel.Result.GetResult();
                    }
                }
            }
            return null;
        }
    }
}
