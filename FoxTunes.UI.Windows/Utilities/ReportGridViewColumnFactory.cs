using FoxTunes.Interfaces;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace FoxTunes
{
    public class ReportGridViewColumnFactory
    {
        public ReportGridViewColumnFactory(IReportComponent source)
        {
            this.Source = source;
        }

        public IReportComponent Source { get; private set; }

        public GridViewColumn Create(int index)
        {
            var gridViewColumn = new GridViewColumn();
            gridViewColumn.Header = this.Source.Headers[index];
            gridViewColumn.DisplayMemberBinding = new Binding()
            {
                Path = new PropertyPath(string.Format("Values[{0}]", index))
            };
            return gridViewColumn;
        }
    }
}
