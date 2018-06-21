namespace TACWpfCustomControls
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Windows.Input;

    public class ViewModel : INotifyPropertyChanged
    {
        private readonly ObservableCollection<string> selectedItems = new ObservableCollection<string>();
        private string summary;

        public ViewModel()
        {
            this.selectedItems.CollectionChanged += (sender, e) => this.UpdateSummary();
        }

        public IEnumerable<string> AllItems { get; set; }

        public IEnumerable<string> InitSelectedItems
        {
            get
            {
                return SelectedItems;
            }
            set
            {
                if (value == null || !value.Any()) return;

                this.SelectedItems.Clear();
                foreach (var item in value)
                {
                    this.SelectedItems.Add(item);
                }
                //OnPropertyChanged("SelectedNames");
            }
        }

        public ObservableCollection<string> SelectedItems
        {
            get
            {
                return this.selectedItems;
            }
        }

        public string Summary
        {
            get
            {
                return this.summary;
            }
            private set
            {
                this.summary = value;
                this.OnPropertyChanged("Summary");
            }
        }

        public ICommand SelectAll
        {
            get
            {
                return new RelayCommand(
                    parameter =>
                    {
                        this.SelectedItems.Clear();
                        foreach (var item in this.AllItems)
                        {
                            this.SelectedItems.Add(item);
                        }
                    });
            }
        }

        protected void OnPropertyChanged(string propertyName)
        {
            var handler = this.PropertyChanged;

            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private void UpdateSummary()
        {
            var sb = new StringBuilder();
            foreach (var selectedName in this.SelectedItems)
            {
                sb.Append(selectedName);
                sb.Append(',');
            }
            this.Summary = string.Format("{0} items are selected.", this.SelectedItems.Count);
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion
    }
}
