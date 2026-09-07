using System.Threading.Tasks;

namespace ECommerce522.APIV9.Repositories
{
    public class ProductSubImageRepository : Repository<ProductSubImage>, IProductSubImageRepository
    {
        public ProductSubImageRepository(ApplicationDbContext context) : base(context)
        {
        }

        public void RemoveRange(IEnumerable<ProductSubImage> items)
        {
            _context.ProductSubImages.RemoveRange(items);
        }

        public async Task AddRangeAsync(IEnumerable<ProductSubImage> items, CancellationToken cancellationToken = default)
        {
            await _context.AddAsync(items, cancellationToken);
        }
    }
}
