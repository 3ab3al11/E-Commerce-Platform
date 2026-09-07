using Microsoft.EntityFrameworkCore;

namespace ECommerce522.Repositories.IRepositories
{
    public interface IProductSubImageRepository : IRepository<ProductSubImage>
    {
        void RemoveRange(IEnumerable<ProductSubImage> items);
        Task AddRangeAsync(IEnumerable<ProductSubImage> items, CancellationToken cancellationToken = default);
    }
}
