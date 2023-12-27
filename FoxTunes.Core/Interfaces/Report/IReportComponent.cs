using System;
using System.Collections.Generic;

namespace FoxTunes.Interfaces
{
    public interface IReportComponent : IInvocableComponent
    {
        string Title { get; }

        event EventHandler TitleChanged;

        string Description { get; }

        event EventHandler DescriptionChanged;

        string[] Headers { get; }

        event EventHandler HeadersChanged;

        IEnumerable<IReportComponentRow> Rows { get; }

        event EventHandler RowsChanged;

        bool IsDialog { get; }
    }

    public interface IReportComponentRow : IInvocableComponent
    {
        string[] Values { get; }

        event EventHandler ValuesChanged;
    }
}
