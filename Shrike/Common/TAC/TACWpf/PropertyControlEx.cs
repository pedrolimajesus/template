using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using PropertyTools.Wpf;

namespace TAC.Wpf
{
    public class PropertyControlEx: PropertyControl
    {
        public static readonly DependencyProperty DefaultTabNameProperty =
            DependencyProperty.Register(
                "DefaultTabName", typeof(string), typeof(PropertyControlEx), new UIPropertyMetadata("Item", DefaultTabNameChanged));

        private static void DefaultTabNameChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            
            var that = dependencyObject as PropertyControlEx;
            var pif = that.PropertyItemFactory as DefaultPropertyItemFactory;
            if (null != pif)
            {
                pif.DefaultTabName = (string) dependencyPropertyChangedEventArgs.NewValue;
            }
        }

        public string DefaultTabName
        {
            get
            {
               
                return (string) this.GetValue(DefaultTabNameProperty);
            }

            set
            {
                this.SetValue(DefaultTabNameProperty, value);
               
            }

        }

        public PropertyControlEx()
        {
            this.PropertyControlFactory = new ExtendedPropertyControlFactory();
        }
    }
}
