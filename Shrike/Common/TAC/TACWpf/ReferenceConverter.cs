using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;

using AppComponents;

namespace TAC.Wpf
{
    using System.ComponentModel;

    public interface IReferenceObject
    {
        bool IsEventRegistered { get; set; }
        Func<string, string> LookupFunction { get; set; }

        Func<IReferenceObject, IList<string>> GetListFunction { get; set; }

        event EventHandler<ReferenceEventArgs> ListUpdatedEvent;

        void OnListUpdatedEvent(ReferenceEventArgs args);
    }

    public class ReferenceEventArgs : EventArgs
    {
        public ReferenceEventArgs(Func<IReferenceObject, IList<string>> getListFunction)
        {
            GetListFunction = getListFunction;
        }

        public Func<IReferenceObject, IList<string>> GetListFunction { get; set; }
    }

    public class ReferenceObject : IReferenceObject
    {
        #region Implementation of IReferenceObject

        public bool IsEventRegistered { get; set; }

        public Func<string, string> LookupFunction { get; set; }

        public Func<IReferenceObject, IList<string>> GetListFunction { get; set; }

        public event EventHandler<ReferenceEventArgs> ListUpdatedEvent;

        public void OnListUpdatedEvent(ReferenceEventArgs args)
        {
            var event1 = ListUpdatedEvent;
            if (event1 != null)
            {
                event1(this, args);
            }
        }

        #endregion
    }

    public interface IReferenceToSummaryLookups
    {
        void Register(string lookupKey, IReferenceObject referenceObject);

        string Lookup(string lookupKey, string referenceKey);

        IList<string> GetList(string lookupKey);
        void RegisterListUpdateEventHandler(string lookupKey, EventHandler<ReferenceEventArgs> eventHandler);
    }

    public static class ReferenceLookupList
    {
        public static IEnumerable GetCompleteList(string lookupKey)
        {
            var refLookup = Catalog.Factory.Resolve<IReferenceToSummaryLookups>();
            var list = refLookup.GetList(lookupKey);
            return list;
        }

        public static void RegisterListUpdateEventHandler(string lookupKey, EventHandler<ReferenceEventArgs> eventHandler)
        {
            var refLookup = Catalog.Factory.Resolve<IReferenceToSummaryLookups>();
            refLookup.RegisterListUpdateEventHandler(lookupKey, eventHandler);
        }
    }

    public class ReferenceToSummaryLookups : IReferenceToSummaryLookups
    {
        private readonly ConcurrentDictionary<string, IReferenceObject> referenceObjects =
            new ConcurrentDictionary<string, IReferenceObject>();

        public void Register(string lookupKey, IReferenceObject referenceObject)
        {
            referenceObjects.TryAdd(lookupKey, referenceObject);
        }

        public string Lookup(string lookupKey, string referenceKey)
        {
            string retval = null;
            IReferenceObject lookup;
            if (referenceObjects.TryGetValue(lookupKey, out lookup))
            {
                retval = lookup.LookupFunction(referenceKey);
            }

            return retval;
        }

        public IList<string> GetList(string lookupKey)
        {
            IList<string> retval = null;
            IReferenceObject lookup;
            if (referenceObjects.TryGetValue(lookupKey, out lookup))
            {
                retval = lookup.GetListFunction(lookup);
            }

            return retval;
        }

        public void RegisterListUpdateEventHandler(string lookupKey, EventHandler<ReferenceEventArgs> eventHandler)
        {
            IReferenceObject lookup;
            if (referenceObjects.TryGetValue(lookupKey, out lookup))
            {
                lookup.ListUpdatedEvent += eventHandler;
            }
        }
    }

    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = true)]
    public sealed class ReferenceLookupAttribute : Attribute
    {
        public string LookupKey { get; set; }

        public ReferenceLookupAttribute(string lookupKey)
        {
            LookupKey = lookupKey;
        }
    }

    public class ReferenceLookupConverter : IValueConverter
    {
        public string LookupKey { get; set; }

        private IReferenceToSummaryLookups _lookups;

        public ReferenceLookupConverter()
        {
            _lookups = Catalog.Factory.Resolve<IReferenceToSummaryLookups>();
        }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var aString = value as string;
            return aString == null ? null : _lookups.Lookup(LookupKey, aString);
        }

        public object ConvertBack(
            object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var backVal = value as string;
            return backVal == null ? null : backVal.Split(':').FirstOrDefault();
        }
    }

    public class ReferenceListLookupConverter : IValueConverter
    {
        public string LookupKey { get; set; }

        private IReferenceToSummaryLookups _lookups;

        public ReferenceListLookupConverter()
        {
            _lookups = Catalog.Factory.Resolve<IReferenceToSummaryLookups>();
        }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null) return null;

            var anID = value as string;
            if (anID != null) //Guid is not IEnumerable
            {
                return (anID + ": " + _lookups.Lookup(LookupKey, anID));
            }

            var items = value as IEnumerable;

            if (null == items)
            {
                return null;
            }

            var list = new List<string>();

            list.AddRange(
                from object it in items
                let itString = it.ToString()
                select (itString + ": " + _lookups.Lookup(this.LookupKey, itString)));


            if (targetType == typeof(BindingList<string>))
            {
                return new BindingList<string>(list);
            }
            return list;
        }

        public object ConvertBack
            (
            object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture
            )
        {
            var backVal = value as string;
            if (backVal != null)
            {
                return backVal.Split(':').FirstOrDefault();
            }
            
            var enumerable = value as IEnumerable;
            if (enumerable == null) return null;

            var list = (from string item in enumerable select item.Split(':').First()).ToList();

            if (targetType == typeof(BindingList<string>))
            {
                return new BindingList<string>(list);
            }
            return list;
        }
    }
}
