using System.Collections.Generic;

namespace FoxTunes.Interfaces
{
    public interface IFilterParserProvider : IStandardComponent
    {
        bool TryParse(ref string filter, out IEnumerable<IFilterParserResultGroup> groups);
    }
}
