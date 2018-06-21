using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Shrike.Areas.TagsUI.TagsUI.Models
{
    public static class TagExtension
    {

        public static bool ContainsValue(this IEnumerable<Tag> list, string value)
        {
            foreach (var tag in list)
            {
                if (tag.Name.ToLower().Contains(value.ToLower()))
                {
                    return true;
                }
            }
            return false;
        }

        public static bool ContainsCategoryValue(this IEnumerable<Tag> list, Tuple<string, string> catValue)
        {
            foreach (var tag in list)
            {
                if (tag.Category.ToLower().Contains(catValue.Item1.ToLower()) && 
                    tag.Name.ToLower().Contains(catValue.Item2.ToLower()))
                {
                    return true;
                }
            }
            return false;
        }

        public static List<Tag> Filter(this List<Tag> list, Tuple<string, string> catValue) 
        {
            List<Tag> response = new List<Tag>();

            foreach(var tag in list)
            {
                if(tag.Category.ToLower().Contains(catValue.Item1.ToLower()) 
                    && tag.Name.ToLower().Contains(catValue.Item2.ToLower()))
                {
                    response.Add(tag);
                }
            }
            return response;
        }

        public static List<Tag> Filter(this IEnumerable<Tag> list, Func<Tag, bool> function ) 
        {
            List<Tag> response = new List<Tag>();

            foreach(var tag in list)
            {
                if (function.Invoke(tag)) 
                {
                    response.Add(tag);
                };
            }
            return response;
        }
    }
}