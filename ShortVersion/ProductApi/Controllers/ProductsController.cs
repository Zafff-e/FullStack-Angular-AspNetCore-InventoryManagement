using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductApi.Dtos;
using ProductApi.Models;
using System.Text.Json.Serialization;

namespace ProductApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly ProductDbContext _context;
        private readonly IWebHostEnvironment _env;

        public ProductsController(ProductDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Product>>> GetAllProducts()
        {
            var products = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brands)
                .AsNoTracking()
                .ToListAsync();

            return Ok(products);
        }

        [HttpGet("categories")]
        public async Task<ActionResult<IEnumerable<Category>>> GetCategories()
        {
            var categories = await _context.Categories.ToListAsync();
            return Ok(categories);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Product>> GetProductById(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brands)
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product == null) return NotFound();
            return Ok(product);
        }

        [HttpPost]
        //{"BrandName":"Brand A","Stock":10},{"BrandName":"Brand B","Stock":20}
        public async Task<IActionResult> PostProduct([FromForm] ProductDto dto)
        {
            var categoryExists = await _context.Categories.AnyAsync(c => c.CategoryId == dto.CategoryId);
            if (!categoryExists)
            {
                return BadRequest("Category does not exist");
            }

            string fileName = "noimage.png";
            if (dto.ImageFile != null && dto.ImageFile.Length > 0)
            {
                fileName = await SaveImageAsync(dto.ImageFile);
            }
            else if (!string.IsNullOrEmpty(dto.ImageUrl))
            {
                fileName = dto.ImageUrl;
            }

            var product = new Product
            {
                ProductName = dto.ProductName,
                ManufacturingDate = dto.ManufacturingDate,
                SerialNo = dto.SerialNo,
                ImageUrl = fileName,
                IsAvailable = dto.IsAvailable,
                CategoryId = dto.CategoryId,
                Price = dto.Price
            };

            if (!string.IsNullOrWhiteSpace(dto.BrandsString))
            {
                try
                {
                    string rawJson = dto.BrandsString.Trim();
                    if (rawJson.StartsWith("{") && rawJson.EndsWith("}"))
                    {
                        rawJson = $"[{rawJson}]";
                    }
                    var options = new System.Text.Json.JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        NumberHandling = JsonNumberHandling.AllowReadingFromString
                    };

                    var brands = System.Text.Json.JsonSerializer.Deserialize<List<BrandDto>>(rawJson, options);
                    if (brands != null)
                    {
                        foreach (var item in brands)
                        {
                            product.Brands.Add(new Brand
                            {
                                BrandName = item.BrandName,
                                Stock = item.Stock
                            });
                        }
                    }
                }
                catch (System.Text.Json.JsonException ex)
                {
                    return BadRequest($"JSON Parsing Error: {ex.Message}. Received: {dto.BrandsString}");
                }
            }
            else if (dto.Brands != null && dto.Brands.Count > 0)
            {
                foreach (var item in dto.Brands)
                {
                    product.Brands.Add(new Brand
                    {
                        BrandName = item.BrandName,
                        Stock = item.Stock
                    });
                }
            }

            _context.Products.Add(product);
            await _context.SaveChangesAsync();
            return Ok(product);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutProduct(int id, [FromForm] ProductDto dto)
        {
            if (id == 0) return BadRequest();
            if (dto.ProductId != 0 && id != dto.ProductId) return BadRequest();

            var existingProduct = await _context.Products
                .Include(p => p.Brands)
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (existingProduct == null) return NotFound();

            var categoryExists = await _context.Categories.AnyAsync(c => c.CategoryId == dto.CategoryId);
            if (!categoryExists)
            {
                return BadRequest("Category does not exist");
            }

            string fileName = existingProduct.ImageUrl;
            if (dto.ImageFile != null && dto.ImageFile.Length > 0)
            {
                fileName = await SaveImageAsync(dto.ImageFile);
                var oldImagePath = Path.Combine(_env.WebRootPath, "images", Path.GetFileName(existingProduct.ImageUrl ?? ""));
                if (Path.GetFileName(existingProduct.ImageUrl) != "noimage.png" && System.IO.File.Exists(oldImagePath))
                {
                    System.IO.File.Delete(oldImagePath);
                }
            }

            existingProduct.ProductId = id;
            existingProduct.ProductName = dto.ProductName;
            existingProduct.ManufacturingDate = dto.ManufacturingDate;
            existingProduct.SerialNo = dto.SerialNo;
            existingProduct.ImageUrl = fileName;
            existingProduct.IsAvailable = dto.IsAvailable;
            existingProduct.CategoryId = dto.CategoryId;
            existingProduct.Price = dto.Price;

            var brands = existingProduct.Brands;
            _context.Brands.RemoveRange(brands);

            if (!string.IsNullOrWhiteSpace(dto.BrandsString))
            {
                try
                {
                    string rawJson = dto.BrandsString.Trim();
                    if (rawJson.StartsWith("{") && rawJson.EndsWith("}"))
                    {
                        rawJson = $"[{rawJson}]";
                    }
                    var options = new System.Text.Json.JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        NumberHandling = JsonNumberHandling.AllowReadingFromString
                    };

                    var newBrands = System.Text.Json.JsonSerializer.Deserialize<List<BrandDto>>(rawJson, options);
                    if (newBrands != null)
                    {
                        foreach (var item in newBrands)
                        {
                            existingProduct.Brands.Add(new Brand
                            {
                                ProductId = id,
                                BrandName = item.BrandName,
                                Stock = item.Stock
                            });
                        }
                    }
                }
                catch (System.Text.Json.JsonException ex)
                {
                    return BadRequest($"JSON Parsing Error: {ex.Message}. Received: {dto.BrandsString}");
                }
            }
            else if (dto.Brands != null && dto.Brands.Count > 0)
            {
                foreach (var item in dto.Brands)
                {
                    existingProduct.Brands.Add(new Brand
                    {
                        ProductId = id,
                        BrandName = item.BrandName,
                        Stock = item.Stock
                    });
                }
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Products.Any(e => e.ProductId == id)) return NotFound();
                throw;
            }

            return NoContent();
        }

        private async Task<string> SaveImageAsync(IFormFile imageFile)
        {
            string uploadDir = Path.Combine(_env.WebRootPath, "images");
            if (!Directory.Exists(uploadDir))
            {
                Directory.CreateDirectory(uploadDir);
            }
            string fileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
            string filePath = Path.Combine(uploadDir, fileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await imageFile.CopyToAsync(stream);
            }
            return fileName;
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            var imageFileName = Path.GetFileName(product.ImageUrl ?? "");
            if (imageFileName != "noimage.png" && !string.IsNullOrEmpty(imageFileName))
            {
                var imagePath = Path.Combine(_env.WebRootPath, "images", imageFileName);
                if (System.IO.File.Exists(imagePath))
                {
                    System.IO.File.Delete(imagePath);
                }
            }

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
