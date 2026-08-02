using System;
using System.Collections.Generic;

namespace SMS.Application.Common
{
    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = new List<T>();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int Page { get { return PageNumber; } set { PageNumber = value; } }
        public int TotalPages { get { return PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0; } set { } }
        public int PageIndex { get { return PageNumber; } set { PageNumber = value; } }
        public int TotalRecords { get { return TotalCount; } set { TotalCount = value; } }
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;
    }
}
