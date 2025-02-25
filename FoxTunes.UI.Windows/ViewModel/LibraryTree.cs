using System.Windows;

namespace FoxTunes.ViewModel
{
    public class LibraryTree : LibraryBase
    {
        protected override Freezable CreateInstanceCore()
        {
            return new LibraryTree();
        }
    }
}
