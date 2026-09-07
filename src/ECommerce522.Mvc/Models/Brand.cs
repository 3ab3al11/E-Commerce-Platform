using System.ComponentModel.DataAnnotations;

namespace ECommerce522.Models
{
    public class Brand
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Img { get; set; } = "defaultBrand.png";
        public string? Description { get; set; }

        public bool Status { get; set; } = true;
    }
}
