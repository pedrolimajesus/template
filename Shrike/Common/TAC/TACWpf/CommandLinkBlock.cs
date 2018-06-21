using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using PropertyTools.DataAnnotations;
using PropertyTools.Wpf;

namespace TAC.Wpf
{
    public class CommandProperty
    {
        public CommandProperty()
        {
            Enabled = true;
        }

        public Action Invoke { get; set; }
        public Action<object> InvokeData { get; set; }

        public bool Enabled { get; set; }
        
        public string LinkText { get; set; }

        public object Data { get; set; }

        public override string ToString()
        {
            return LinkText;
        }
    }


    public interface IPrefabCommand
    {
        void MouseDown(CommandProperty cp);
    
    }

    /// <summary>
    /// The attributed command property, when clicked, will
    /// create a property dialog from the data member of the property
    /// and then call the invokearg delegate of the property 
    /// if the user clicks ok, with the data as the argument.
    /// If PreInvoke is true, then it will attempt to 
    /// call the invoke delegate of the property first (which
    /// may allow for setting the data member before the data is
    /// shown)
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class PrefabDataFormAttribute : Attribute, IPrefabCommand
    {
      
        public void MouseDown(CommandProperty cp)
        {

            if (null != cp.Invoke)
                cp.Invoke();

            var dlg = new PropertyDialog
            {
                DataContext = cp.Data,
                Title = cp.LinkText,
                MinHeight = 500
            };
            
            if (dlg.ShowDialog().Value)
            {
                if (null != cp.InvokeData)
                {
                    cp.InvokeData(cp.Data);
                }
            }
        }
    }


    /// <summary>
    /// The attributed command property, when clicked, will
    /// create a property dialog. The data member will have the given
    /// form type, and the data member of the command property
    /// will be set on the given form property.
    /// It will call the invokearg delegate of the property 
    /// if the user clicks ok, with the data as the argument.
    /// If PreInvoke is true, then it will attempt to 
    /// call the invoke delegate of the property first (which
    /// may allow for setting the data member before the data is
    /// shown)
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class PrefabWrapFormAttribute : Attribute, IPrefabCommand
    {
        public PrefabWrapFormAttribute(Type formType, string formProperty)
        {
            FormType = formType;
            FormProperty = formProperty;
        }
        
        public Type FormType { get; set; }
        public string FormProperty { get; set; }



        public void MouseDown(CommandProperty cp)
        {
            if (null != cp.Invoke)
                cp.Invoke();

            var form = Activator.CreateInstance(FormType);
            FormType.GetProperty(FormProperty).SetValue(form,cp.Data,null);

            var dlg = new PropertyDialog
            {
                DataContext = form,
                Title = cp.LinkText,
                MinHeight = 500
            };

            if (dlg.ShowDialog().Value)
            {
                if (null != cp.InvokeData)
                {
                    cp.InvokeData(form);
                }
            }
        }
    }


    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class PrefabListAppenderFormAttribute : Attribute, IPrefabCommand
    {
        public PrefabListAppenderFormAttribute(Type itemType)
        {
            ItemType = itemType;
        }

        public Type ItemType { get; set; }

        
        public void MouseDown(CommandProperty cp)
        {
            
            if (null != cp.Invoke)
                cp.Invoke();

            var list = cp.Data as IList;
            if (null == list)
                return;

            var form = Activator.CreateInstance(ItemType);
            

            var dlg = new PropertyDialog
            {
                DataContext = form,
                Title = cp.LinkText,
                MinHeight = 500
            };

            if (dlg.ShowDialog().Value)
            {
                if (null != cp.InvokeData)
                {
                    cp.InvokeData(form);
                }

                list.Add(form);
            }
            
        }
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class PrefabWrapListAppenderFormAttribute : Attribute, IPrefabCommand
    {
        public PrefabWrapListAppenderFormAttribute(Type formType, string dataProperty)
        {
            FormType = formType;
            DataProperty = dataProperty;
        }

        public Type FormType { get; set; }
        public string DataProperty { get; set; }


        public void MouseDown(CommandProperty cp)
        {

            if (null != cp.Invoke)
                cp.Invoke();

            var list = cp.Data as IList;
            if (null == list)
                return;

            var form = Activator.CreateInstance(FormType);


            var dlg = new PropertyDialog
            {
                DataContext = form,
                Title = cp.LinkText,
                MinHeight = 500
            };

            if (dlg.ShowDialog().Value)
            {
                if (null != cp.InvokeData)
                {
                    
                    cp.InvokeData(form);
                }

                var newItem = FormType.GetProperty(DataProperty).GetValue(form, null);

                list.Add(newItem);
            }

        }
    }


    internal class PickListForm
    {
        [Browsable(false)]
        public List<string> PickList { get; set; }

        [ItemsSourceProperty("PickList")]
        public string SelectItem { get; set; }
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class PrefabPickListFormAttribute : Attribute, IPrefabCommand
    {
        public PrefabPickListFormAttribute(Type dataConverter)
        {
            
        }

        
        public Type DataConverter { get; set; }
    

        public void MouseDown(CommandProperty cp)
        {
            if (null != cp.Invoke)
                cp.Invoke();

            var list = cp.Data as IEnumerable;
            if (null == list)
                return;

            var converter = Activator.CreateInstance(DataConverter) as IValueConverter;
            if(null == converter)
                return;
            
            var converted = new List<string>();
            Type sourceType = null;

            foreach (var it in list)
            {

                var convertedItem = converter.Convert(it, typeof (string), cp, CultureInfo.CurrentCulture) as string;
                if(null != convertedItem) converted.Add(convertedItem);
                if(null == sourceType)
                    sourceType = it.GetType();
            }

            

            var form = new PickListForm
                           {
                               PickList = converted
                           };

            var dlg = new PropertyDialog
            {
                DataContext = form,
                Title = cp.LinkText,
                MinHeight = 500
            };

            if (dlg.ShowDialog().Value)
            {
                if (null != cp.InvokeData)
                {
                    var convertedBack = converter.ConvertBack(form.SelectItem, sourceType ?? typeof (string), cp,
                                                              CultureInfo.CurrentCulture);
                    if(null != cp.InvokeData) cp.InvokeData(convertedBack);
                }

            }
        }
    }

    

    [AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
    internal sealed class AssignCommandDataAttribute : Attribute
    {


        // This is a positional argument
        public AssignCommandDataAttribute()
        {
           
        }

        
    }
    

    /// <summary>
    /// Provides a lightweight control for displaying hyperlinks.
    /// </summary>
    public class CommandLinkBlock : TextBlock
    {


        /// <summary>
        /// The navigate uri property.
        /// </summary>
        public static readonly DependencyProperty CommandProperty = DependencyProperty.Register(
            "Command", typeof(CommandProperty), typeof(CommandLinkBlock), new UIPropertyMetadata(null));

        public static readonly DependencyProperty CommandDataProperty = DependencyProperty.Register(
            "CommandData", typeof (object), typeof (CommandLinkBlock), new UIPropertyMetadata(null));

        static CommandLinkBlock()
        {
            CursorProperty.OverrideMetadata(typeof(CommandLinkBlock), new FrameworkPropertyMetadata(Cursors.Hand));
            TextDecorationsProperty.OverrideMetadata(typeof(CommandLinkBlock), new FrameworkPropertyMetadata(System.Windows.TextDecorations.Underline));
        }

        public CommandLinkBlock()
        {
            
        }

        public IPrefabCommand PrefabInvocation { get; set; }
        public bool CommandDataAssignment { get; set; }

        /// <summary>
        /// Gets or sets the navigation URI.
        /// </summary>
        /// <value> The navigate URI. </value>
        public CommandProperty Command
        {
            get
            {
                return (CommandProperty) GetValue(CommandProperty);
            }

            set
            {
                SetValue(CommandProperty, value);
                if(null == value)
                    this.Visibility = Visibility.Collapsed;
            }
        }

        public object CommandData
        {
            get { return GetValue(CommandDataProperty); }

            set
            {
                SetValue(CommandDataProperty, value);
            }
        }

        /// <summary>
        /// Invoked when an unhandled <see cref="E:System.Windows.Input.Mouse.MouseDown"/>�attached event reaches an element in its route that is derived from this class. Implement this method to add class handling for this event.
        /// </summary>
        /// <param name="e">
        /// The <see cref="T:System.Windows.Input.MouseButtonEventArgs"/> that contains the event data. This event data reports details about the mouse button that was pressed and the handled state.
        /// </param>
        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);
            if (Command != null && Command.Enabled)
            {
                if (CommandDataAssignment)
                {
                    Command.Data = CommandData;
                }

                if (null != PrefabInvocation)
                {
                    PrefabInvocation.MouseDown(Command);
                    return;
                }

                if(Command.Invoke != null)
                    Command.Invoke();

                if (Command.InvokeData != null)
                    Command.InvokeData(Command.Data);
            }
        }
    }

    
}
