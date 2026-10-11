using System.ComponentModel.DataAnnotations;

namespace InventoryAPIProject.DTOs
{
    public class ProductDto
    {
        public int ProductId { get; set; }

        [Required, StringLength(100)]
        public string ProductName { get; set; } = string.Empty;

        public DateOnly ManufacturingDate { get; set; }

        [Required, StringLength(50)]
        public string SerialNo { get; set; } = string.Empty;

        public string? ImageUrl { get; set; }

        public bool IsAvailable { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Please select a category")]
        public int CategoryId { get; set; }

        public string? CategoryName { get; set; }   // get korar somoy pabe but fill korar somoy pabe na

        [Range(0.01, 1000000, ErrorMessage = "Price must be greater than 0")]
        public decimal Price { get; set; }

        public List<BrandDto> Brands { get; set; } = new List<BrandDto>();
    }
}
