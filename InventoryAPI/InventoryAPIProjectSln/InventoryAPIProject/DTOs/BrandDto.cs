using System.ComponentModel.DataAnnotations;

namespace InventoryAPIProject.DTOs
{
    public class BrandDto
    {
        public int BrandId { get; set; }

        [Required, StringLength(100)]
        public string BrandName { get; set; } = string.Empty;

        [Range(0, 100000)]
        public int Stock { get; set; }
    }
}
