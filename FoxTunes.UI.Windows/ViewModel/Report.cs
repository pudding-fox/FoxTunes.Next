using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace FoxTunes.ViewModel
{
    public class Report : ViewModelBase
    {
        public ReportGridViewColumnFactory GridViewColumnFactory { get; private set; }

        private IReportComponent _Source { get; set; }

        public IReportComponent Source
        {
            get
            {
                return this._Source;
            }
            set
            {
                this.OnSourceChanging();
                this._Source = value;
                this.OnSourceChanged();
            }
        }

        protected virtual void OnSourceChanging()
        {
            if (this.Source != null)
            {
                this.Source.RowsChanged -= this.OnRowsChanged;
            }
        }

        protected virtual void OnSourceChanged()
        {
            if (this.Source != null)
            {
                this.Source.RowsChanged += this.OnRowsChanged;
            }
            this.Refresh();
            if (this.SourceChanged != null)
            {
                this.SourceChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("Source");
        }

        public event EventHandler SourceChanged;

        public IEnumerable<GridViewColumn> GridColumns
        {
            get
            {
                if (this.Source != null && this.GridViewColumnFactory != null)
                {
                    for (var a = 0; a < this.Source.Headers.Length; a++)
                    {
                        yield return this.GridViewColumnFactory.Create(a);
                    }
                }
            }
        }

        protected virtual void OnGridColumnsChanged()
        {
            if (this.GridColumnsChanged != null)
            {
                this.GridColumnsChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("GridColumns");
        }

        public event EventHandler GridColumnsChanged;

        private IReportComponentRow _SelectedRow { get; set; }

        public IReportComponentRow SelectedRow
        {
            get
            {
                return this._SelectedRow;
            }
            set
            {
                this._SelectedRow = value;
                this.OnSelectedRowChanged();
            }
        }

        protected virtual void OnSelectedRowChanged()
        {
            if (this.SelectedRow != null)
            {
                var task = this.SelectedRow.TryInvoke(ReportComponent.ReportComponentRow.SELECT);
            }
            if (this.SelectedRowChanged != null)
            {
                this.SelectedRowChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("SelectedRow");
        }

        public event EventHandler SelectedRowChanged;

        public ICommand RowActivateCommand
        {
            get
            {
                return new AsyncCommand(this.RowActivate);
            }
        }

        public Task RowActivate()
        {
            return this.SelectedRow.TryInvoke(ReportComponent.ReportComponentRow.ACTIVATE);
        }

        public ICommand AcceptCommand
        {
            get
            {
                return new AsyncCommand(this.Accept);
            }
        }

        public Task Accept()
        {
            return this.Source.TryInvoke(ReportComponent.ACCEPT, this.SelectedRow);
        }

        public string AcceptText
        {
            get
            {
                var invocation = default(IInvocationComponent);
                if (this.Source.TryGetInvocation(ReportComponent.ACCEPT, out invocation))
                {
                    return invocation.Name;
                }
                return Strings.Report_Close;
            }
        }

        protected virtual void OnAcceptTextChanged()
        {
            if (this.AcceptTextChanged != null)
            {
                this.AcceptTextChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("AcceptText");
        }

        public event EventHandler AcceptTextChanged;

        protected virtual void Refresh()
        {
            if (this.GridViewColumnFactory == null)
            {
                this.GridViewColumnFactory = new ReportGridViewColumnFactory(this.Source);
            }
            this.OnGridColumnsChanged();
            this.OnAcceptTextChanged();
        }

        protected virtual void OnRowsChanged(object sender, EventArgs e)
        {
            var task = Windows.Invoke(this.Refresh);
        }

        protected override Freezable CreateInstanceCore()
        {
            return new Report();
        }

        protected override void OnDisposing()
        {
            if (this.Source != null)
            {
                this.Source.RowsChanged -= this.OnRowsChanged;
            }
            base.OnDisposing();
        }
    }
}
