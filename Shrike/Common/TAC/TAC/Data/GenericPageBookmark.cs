using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AppComponents.Data
{
    public class GenericPageBookmark: IPageBookmark
    {
        public GenericPageBookmark(IPageBookmark pbm)
        {
            PageSize = pbm.PageSize;
            CurrentPage = pbm.CurrentPage;
            LastSkippedResults = pbm.LastSkippedResults;
            TotalResults = pbm.TotalResults;
            More = pbm.More;
        }

        public GenericPageBookmark()
        {
            PageSize = 100;
            CurrentPage = 0;
            LastSkippedResults = 0;
            TotalResults = 0;
            More = true;

        }

        public int PageSize
        {
            get; set;
          
        }

        public int CurrentPage { get; set; }

        public int LastSkippedResults { get; set; }

        public int TotalResults { get; set; }

        public int TotalPages
        {
            get 
            { 
                if (PageSize == 0) return 0; 
                return TotalResults / PageSize; 
            }
        }

        public void Forward()
        {
            CurrentPage++;
            TotalResults = PageSize;

        }

        public bool More { get; set; }

        public bool Done
        {
            get { return !More; }
        }

        
    }

    public static class GenericPaging
    {
        
        public static IQueryable<T> Page<T>(IQueryable<T> that, IPageBookmark bm)
        {
            if (bm.CurrentPage > 0)
            {
                return 
                       that
                           .Skip((bm.CurrentPage * bm.PageSize) + bm.LastSkippedResults)
                           .Take(bm.PageSize);
            }
            else
            {
                return 
                       that
                           .Take(bm.PageSize);
            }
        }

        public static IEnumerable<T> PageAll<T>(IQueryable<T> that, IPageBookmark bm )
        {
            var data = Enumerable.Empty<T>();
            do
            {
                var moreData = Page(that, bm).AsEnumerable();
                if (moreData.Any())
                    data = data.Concat(moreData);
                bm.Forward();
                bm.More = moreData.Any();
            } while (bm.More);

            return data;

        }


    }
}
