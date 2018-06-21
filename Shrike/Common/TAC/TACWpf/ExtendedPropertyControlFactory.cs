using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;
using System.Xml;
using PropertyTools.DataAnnotations;
using PropertyTools.Wpf;

namespace TAC.Wpf
{
    using System.Windows.Controls.Primitives;

    using TACWpfCustomControls;

    public class ExtendedPropertyControlFactory: DefaultPropertyControlFactory 
    {
        public override FrameworkElement CreateControl(PropertyItem pi, PropertyControlFactoryOptions options)
        {
            FrameworkElement retval = null;
            if (pi.Is(typeof(CommandProperty)))
            {
                pi.HeaderPlacement = HeaderPlacement.Collapsed;
                retval = CreateCommandLinkControl(pi);
            }
            else if (pi.GetAttribute<NestedPropertiesAttribute>() != null)
            {
                if (pi.Is(typeof(ICollection)) || pi.Is(typeof(ICollection<>))) retval = CreateNestedPropertyGridsControl(pi);
                else retval = CreateNestedPropertyGridControl(pi);
            }
            else
            {
                var rla = pi.GetAttribute<ReferenceLookupAttribute>();
                if (rla != null)
                {
                    if (pi.Is(typeof(string)))
                    {
                        pi.Converter = new ReferenceListLookupConverter { LookupKey = rla.LookupKey };
                        pi.ItemsSource = ReferenceLookupList.GetCompleteList(rla.LookupKey);
                        
                        retval = this.CreateBindableComboBoxControl(pi, rla.LookupKey);
                    }
                    else if (pi.Is(typeof(IEnumerable)))
                    {
                        pi.Converter = new ReferenceListLookupConverter { LookupKey = rla.LookupKey };
                        pi.ItemsSource = ReferenceLookupList.GetCompleteList(rla.LookupKey);

                        retval = this.CreateBindableMultipleSelectControl(pi, rla.LookupKey);
                    }
                    else
                    {
                        pi.Converter = new ReferenceLookupConverter { LookupKey = rla.LookupKey };
                        retval = base.CreateDefaultControl(pi);
                    }
                }
                else
                {
                    retval = base.CreateControl(pi, options);
                }
            }

            retval.Name = pi.Descriptor.Name + "PropertyPageElem";

            return retval;
        }

        private FrameworkElement CreateBindableMultipleSelectControl(PropertyItem pi, string lookupKey)
        {
            var control = new ListBoxWrapper();

            var data = pi.Converter.Convert(pi.ItemsSource, typeof(IList<string>), null, null) as IList<string>;
            control.AllItems = data;
            control.InitSelectedItemsViewModel = null;


            ReferenceLookupList.RegisterListUpdateEventHandler(lookupKey, (sender, args) =>
                {
                    var list = ReferenceLookupList.GetCompleteList(lookupKey);
                    var data1 = pi.Converter.Convert(list, typeof(IList<string>), null, null) as IList<string>;
                    control.AllItems = data1;
                });

            var selectedItemBinding = pi.CreateBinding();
            selectedItemBinding.Mode = BindingMode.TwoWay;
            control.SetBinding(ListBoxWrapper.SelectedItemsProperty, selectedItemBinding);

            //var initSelectedBinding = pi.CreateOneWayBinding();
            //control.SetBinding(ListBoxWrapper.InitSelectedItemsProperty, initSelectedBinding);

            return control;
            //return base.CreateDefaultControl(pi);
        }

        private FrameworkElement CreateBindableComboBoxControl(PropertyItem pi, string lookupKey)
        {
            var cbx = new ComboBox();

            var itemsSourceBinding = pi.CreateOneWayBinding();
            itemsSourceBinding.Path = null;
            itemsSourceBinding.Source = pi.ItemsSource;
            cbx.SetBinding(ItemsControl.ItemsSourceProperty, itemsSourceBinding);

            ReferenceLookupList.RegisterListUpdateEventHandler(lookupKey, (sender, args) =>
                {
                    var list = ReferenceLookupList.GetCompleteList(lookupKey);
                    var itemsSourceBinding1 = pi.CreateOneWayBinding();
                    itemsSourceBinding1.Path = null;
                    itemsSourceBinding1.Source = list;
                    cbx.SetBinding(ItemsControl.ItemsSourceProperty, itemsSourceBinding1);
                });

            var selectedItemBinding = pi.CreateBinding();
            selectedItemBinding.Mode = BindingMode.TwoWay;
            //selectedItemBinding.Path = new PropertyPath("Selected");
            cbx.SetBinding(Selector.SelectedItemProperty, selectedItemBinding);

            return cbx;
        }

        private FrameworkElement CreateNestedPropertyGridsControl(PropertyItem pi)
        {
            const string Xaml = @"<Expander xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
             xmlns:mc='http://schemas.openxmlformats.org/markup-compatibility/2006' 
             xmlns:Wpf='clr-namespace:TAC.Wpf;assembly=TACWpf'>
                <ItemsControl Name ='ItemPanel' >
                    <ItemsControl.ItemTemplate>
                        <DataTemplate>
                            <Wpf:PropertyControlEx DefaultTabName='Item' SelectedObject='{Binding }' Margin='8' Padding='12'/>
                        </DataTemplate>
                    </ItemsControl.ItemTemplate>
                </ItemsControl>
             </Expander>";

            var sr = new StringReader(Xaml);
            var reader = XmlReader.Create(sr);
            var retval = (FrameworkElement)XamlReader.Load(reader);

            var ic = (ItemsControl)retval.FindName("ItemPanel");
            if (ic != null)
            {
                ic.SetBinding(ItemsControl.ItemsSourceProperty, pi.CreateBinding());
            }

            return retval;
        }

        protected virtual FrameworkElement CreateNestedPropertyGridControl(PropertyItem pi)
        {
            const string Xaml = @"<Expander xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
             xmlns:mc='http://schemas.openxmlformats.org/markup-compatibility/2006' 
             xmlns:Wpf='clr-namespace:TAC.Wpf;assembly=TACWpf'>
                            <Wpf:PropertyControlEx Name='NestedPanel' Margin='8' Padding='12'/>
             </Expander>";

            var sr = new StringReader(Xaml);
            var reader = XmlReader.Create(sr);
            var retval = (FrameworkElement)XamlReader.Load(reader);

            var pc = (PropertyControlEx)retval.FindName("NestedPanel");
            if (pc != null)
            {
                pc.SetBinding(PropertyControl.SelectedObjectProperty, pi.CreateBinding());
            }

            return retval;


        }

        protected override FrameworkElement CreateDefaultControl(PropertyItem pi)
        {
            var rla = pi.GetAttribute<ReferenceLookupAttribute>();
            if (rla != null)
            {
                if (pi.Is(typeof(string)))
                {
                    pi.Converter = new ReferenceLookupConverter { LookupKey = rla.LookupKey };
                }
                else if (pi.Is(typeof(IEnumerable)))
                {
                    pi.Converter = new ReferenceListLookupConverter { LookupKey = rla.LookupKey };
                }
                else
                {
                    pi.Converter = null;
                }
            }

            return base.CreateDefaultControl(pi);
        }

        protected virtual FrameworkElement CreateCommandLinkControl(PropertyItem property)
        {
            
            var commandAtt = property.Descriptor.Attributes.OfType<Attribute>().FirstOrDefault(att => att is IPrefabCommand) as IPrefabCommand;


            var c = new CommandLinkBlock { PrefabInvocation = commandAtt, VerticalAlignment = VerticalAlignment.Center, TextDecorations = TextDecorations.Underline };
            if (property.GetAttribute<AssignCommandDataAttribute>() != null) c.CommandDataAssignment = true;


            c.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding(property.Descriptor.Name));
            c.SetBinding(CommandLinkBlock.CommandProperty, property.CreateBinding());

            if (c.CommandDataAssignment)
            {
                c.SetBinding(CommandLinkBlock.CommandDataProperty,
                    new System.Windows.Data.Binding("SelectedObject")
                    {
                        RelativeSource =new RelativeSource(RelativeSourceMode.FindAncestor,typeof(PropertyControlEx), 1),Mode = BindingMode.OneWay
                    });
            }

            return c;
        }
    }
}
