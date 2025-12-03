namespace Shared.Request
{
    public abstract class RequestParameters
    {
        const int maxPageSize = 1000000;
        public int PageNumber { get; set; } = 1;
        private int _pageSize = 30;
        public int PageSize
        {
            get
            {
                return _pageSize;
            }
            set
            {
                _pageSize = value > maxPageSize ? maxPageSize : value;
            }
        }
    }
}
