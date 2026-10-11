using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace ProductApi.Dtos
{
    public class ProductDto
    {
        public int ProductId { get; set; }

        [Required, StringLength(100)]
        public string ProductName { get; set; } = string.Empty;

        public DateTime ManufacturingDate { get; set; } = DateTime.Now;

        [Required, StringLength(50)]
        public string SerialNo { get; set; } = string.Empty;

        public string? ImageUrl { get; set; }

        public IFormFile? ImageFile { get; set; }

        public bool IsAvailable { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Please select a category")]
        public int CategoryId { get; set; }

        public string? CategoryName { get; set; }

        [Range(0.01, 1000000, ErrorMessage = "Price must be greater than 0")]
        public decimal Price { get; set; }

        public string? BrandsString { get; set; }

        public List<BrandDto> Brands { get; set; } = new List<BrandDto>();
    }

    public class BrandDto
    {
        public int BrandId { get; set; }

        [Required, StringLength(100)]
        public string BrandName { get; set; } = string.Empty;

        [Range(0, 100000)]
        public int Stock { get; set; }
    }
}
