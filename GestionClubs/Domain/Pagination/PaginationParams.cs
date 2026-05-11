namespace GestionClubs.Domain.Pagination
{
    public class PaginationParams
    {
        private const int MaxPageSize = 100;
        private const int DefaultPageSize = 10;
        private int? _pageSize;
        private int? _pageNumber;

        public int? PageNumber
        {
            get => _pageNumber is null or < 1 ? 1 : _pageNumber;
            set => _pageNumber = value;
        }

        public int? PageSize
        {
            get => _pageSize switch
            {
                null or < 1 => DefaultPageSize,
                > MaxPageSize => MaxPageSize,
                _ => _pageSize
            };
            set => _pageSize = value;
        }
    }
}
