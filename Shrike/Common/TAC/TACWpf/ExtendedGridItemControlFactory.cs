using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using PropertyTools.Wpf;

namespace TAC.Wpf
{
    public class ExtendedGridItemControlFactory : ItemsGridControlFactory 
    {

        public override FrameworkElement CreateDisplayControl(PropertyDefinition d, int index)
        {
            if (d.PropertyType == typeof(CommandProperty))
            {
                return CreateCommandLinkControl(d, index);
            }
            return base.CreateDisplayControl(d, index);
        }

        public override FrameworkElement CreateEditControl(PropertyDefinition d, int index)
        {
            if (d.PropertyType == typeof(CommandProperty))
            {
                return CreateCommandLinkControl(d, index);
            }

            return base.CreateEditControl(d, index);
        }


        protected virtual FrameworkElement CreateCommandLinkControl(PropertyDefinition d, int index)
        {
            var commandAtt =
               d.Descriptor.Attributes.OfType<Attribute>().FirstOrDefault(att => att is IPrefabCommand) as IPrefabCommand;
            
            var cl = new CommandLinkBlock
                         {
                             PrefabInvocation = commandAtt,
                             HorizontalAlignment = d.HorizontalAlignment,
                             VerticalAlignment = VerticalAlignment.Center,
                             Padding = new Thickness(4, 0, 4, 0),
                             TextDecorations = TextDecorations.Underline,
                             
                         };

            if (d.Descriptor.Attributes.OfType<AssignCommandDataAttribute>().Any()) cl.CommandDataAssignment = true;


            cl.SetBinding(TextBlock.TextProperty, new Binding(d.Descriptor.Name));
            cl.SetBinding(CommandLinkBlock.CommandProperty, d.CreateOneWayBinding(index));

            if (cl.CommandDataAssignment)
            {
                cl.SetBinding(CommandLinkBlock.CommandDataProperty, new Binding("SelectedItems")
                                                                        {
                                                                            RelativeSource =
                                                                                new RelativeSource(
                                                                                RelativeSourceMode.FindAncestor,
                                                                                typeof (ItemsGrid), 1),
                                                                            Mode = BindingMode.OneWay
                                                                        });
            }

            return cl;
        }
    }
}
