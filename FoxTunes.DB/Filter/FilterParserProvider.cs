using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace FoxTunes
{
    [ComponentDependency(Slot = ComponentSlots.Database)]
    public abstract class FilterParserProvider : StandardComponent, IFilterParserProvider
    {
        public virtual bool TryParse(ref string filter, out IEnumerable<IFilterParserResultGroup> groups)
        {
            var group = default(IFilterParserResultGroup);
            if (this.TryParse(ref filter, out group))
            {
                groups = new[] { group };
                return true;
            }
            groups = default(IEnumerable<IFilterParserResultGroup>);
            return false;
        }

        public virtual bool TryParse(ref string filter, out IFilterParserResultGroup group)
        {
            throw new NotImplementedException();
        }

        protected virtual void OnParsed(ref string filter, int position, int length)
        {
            var builder = new StringBuilder();
            if (position > 0)
            {
                builder.Append(filter.Substring(0, position));
            }
            if (length > 0 && length < filter.Length)
            {
                builder.Append(filter.Substring(position + length, filter.Length - (position + length)));
            }
            filter = builder.ToString();
        }
    }
}
