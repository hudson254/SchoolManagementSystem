using System.Collections.Generic;

namespace SMS.Shared.DTOs
{
    public class PagedResult<T>
    {
        public PagedResult()
        {
            Items = new List<T>();
        }

        public PagedResult(IEnumerable<T> items, int count, int page, int pageSize)
        {
            Items = items ?? new List<T>();
            TotalCount = count;
            PageNumber = page;
            PageSize = pageSize;
        }

        public IEnumerable<T> Items { get; set; }
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }

        // Backward-compatible settable properties
        public int Page { get; set; }
        public int TotalPages { get; set; }
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;

        public static PagedResult<T> Empty()
        {
            return new PagedResult<T>
            {
                Items = new List<T>(),
                TotalCount = 0,
                PageNumber = 1,
                PageSize = 10
            };
        }
    }
}

