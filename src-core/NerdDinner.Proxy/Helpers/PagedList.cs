using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace NerdDinner.Proxy.Helpers
{
    // Minimal stand-in for the legacy app's X.PagedList dependency (M9,
    // decision-log.md DL-028) -- X.PagedList's newer releases dropped
    // classic-.NET-Framework support entirely (per M1/DL-009), and rather
    // than chase whichever version happens to support both net48 and
    // net10 today, this is a small enough shape (skip/take + the four
    // boolean flags DinnerPagination.cshtml actually reads) to own
    // directly instead of taking on another external dependency.
    public interface IPagedList<T> : IEnumerable<T>
    {
        int PageNumber { get; }
        int PageCount { get; }
        bool IsFirstPage { get; }
        bool IsLastPage { get; }
        bool HasPreviousPage { get; }
        bool HasNextPage { get; }
    }

    public class PagedList<T> : IPagedList<T>
    {
        private readonly List<T> _items;

        public int PageNumber { get; }
        public int PageCount { get; }

        public bool IsFirstPage => PageNumber <= 1;
        public bool IsLastPage => PageNumber >= PageCount;
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < PageCount;

        public PagedList(IQueryable<T> source, int pageNumber, int pageSize)
        {
            PageNumber = pageNumber < 1 ? 1 : pageNumber;

            var totalCount = source.Count();
            PageCount = totalCount == 0 ? 1 : (totalCount + pageSize - 1) / pageSize;

            _items = source.Skip((PageNumber - 1) * pageSize).Take(pageSize).ToList();
        }

        public IEnumerator<T> GetEnumerator() => _items.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
