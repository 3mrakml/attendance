using Attendence_System.ViewModel;

namespace Attendence_System.Extensions
{
    public static class PaginationExtensions
    {
        public static (List<T> Items, PaginationInfo Info) Paginate<T>(
            this IEnumerable<T> source,
            int pageNumber,
            int pageSize,
            string action,
            string controller = null,
            Dictionary<string, string> routeParams = null)
        {
            int totalItems = source.Count();
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            // Ensure pageNumber is within bounds
            if (pageNumber < 1) pageNumber = 1;
            // (Optional) We could restrict to totalPages, but usually empty lists are fine.

            var items = source.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

            var info = new PaginationInfo
            {
                CurrentPage = pageNumber,
                TotalPages = totalPages,
                TotalItems = totalItems,
                Action = action,
                Controller = controller,
                RouteParams = routeParams ?? new Dictionary<string, string>()
            };

            return (items, info);
        }
    }
}
