namespace SMS.Application.DTOs
{
    public class PagedResult<T>
    {
        public IEnumerable<T> Items { get; set; } = new List<T>();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }

        public bool HasPrevious => Page > 1;
        public bool HasNext => Page < TotalPages;

        public PagedResult()
        {
        }

        public PagedResult(IEnumerable<T> items, int count, int page, int pageSize)
        {
            Items = items;
            TotalCount = count;
            Page = page;
            PageSize = pageSize;
            TotalPages = (int)Math.Ceiling(count / (double)pageSize);
        }

        public static PagedResult<T> Empty()
        {
            return new PagedResult<T>
            {
                Items = new List<T>(),
                TotalCount = 0,
                Page = 1,
                PageSize = 10,
                TotalPages = 0
            };
        }
    }
}