namespace TACWpfCustomControls
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Windows;
    using System.Windows.Controls;
    using System.ComponentModel;

    /// <summary>
    /// Interaction logic for ListBoxWrapper1.xaml
    /// </summary>
    public partial class ListBoxWrapper : UserControl
    {
        //private readonly ListBoxEx listBoxEx = new ListBoxEx();
        private static int counter;
        private readonly ViewModel viewModel = new ViewModel();

        public ListBoxWrapper()
        {
            this.InitializeComponent();

            this.listBoxEx.DataContext = this.viewModel;

        }

        public static readonly DependencyProperty SelectedItemsProperty = DependencyProperty.Register(
            "SelectedItems",
            typeof(BindingList<string>),
            typeof(ListBoxWrapper),
            new UIPropertyMetadata(new BindingList<string>(), PropertyChangedCallback));

        private static void PropertyChangedCallback(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            if (counter >= 1)
            {
                return;
            }

            var control = dependencyObject as ListBoxWrapper;
            if (control == null)
            {
                return;
            }

            if (dependencyPropertyChangedEventArgs.NewValue != null)
            {
                control.viewModel.SelectedItems.Clear();
                foreach (string item in (IEnumerable)dependencyPropertyChangedEventArgs.NewValue)
                {
                    control.viewModel.SelectedItems.Add(item);
                }
            }

            control.viewModel.SelectedItems.CollectionChanged += (sender, args) =>
                {
                    control.SelectedItems = new BindingList<string>(control.viewModel.SelectedItems);
                };

            counter++;
        }
            //new FrameworkPropertyMetadata(new BindingList<string>(), test));

        public BindingList<string> SelectedItems
        {
            get
            {
                return (BindingList<string>)this.GetValue(SelectedItemsProperty);
            }
            set
            {
                this.SetValue(SelectedItemsProperty, value);
            }
        }

        public IEnumerable<string> AllItems
        {
            get
            {
                return this.viewModel.AllItems;
            }
            set
            {
                this.viewModel.AllItems = value;
            }
        }

        public IEnumerable<string> InitSelectedItemsViewModel
        {
            set
            {
                this.viewModel.InitSelectedItems = value;
            }
        }
    }
}
