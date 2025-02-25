using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace FoxTunes
{
    public class StringCollection : ObservableCollection<string>
    {
        public StringCollection()
        {

        }

        public StringCollection(IEnumerable<string> collection) : base(collection)
        {

        }
    }
}
