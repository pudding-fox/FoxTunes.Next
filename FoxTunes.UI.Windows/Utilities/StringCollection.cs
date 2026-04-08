using System.Collections.Generic;

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
