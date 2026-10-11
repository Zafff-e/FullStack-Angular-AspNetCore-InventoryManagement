using InventoryAPIProject.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InventoryAPIProject.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly ProductDbContext _db;
        private readonly IWebHostEnvironment _env;

        public ProductsController(ProductDbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        // GET: api/Products
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Product>>> GetProducts()
        {
            var products = await _db.Products.ToListAsync();
            return Ok(products);
        }

        // GET: api/Products/Brand/Include  or api/Products/Include
        [HttpGet("Brand/Include")]
        [HttpGet("Include")]
        public async Task<ActionResult<IEnumerable<Product>>> GetProductsWithBrands()
        {
            var products = await _db.Products
                .Include(p => p.Category)
                .Include(p => p.Brands)
                .ToListAsync();
            return Ok(products);
        }

        // GET: api/Products/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Product>> GetProduct(int id)
        {
            var product = await _db.Products.FindAsync(id);
            if (product == null) return NotFound();
            return Ok(product);
        }

        // GET: api/Products/5/Include
        [HttpGet("{id}/Include")]
        public async Task<ActionResult<Product>> GetProductWithBrands(int id)
        {
            var product = await _db.Products
                .Include(p => p.Category)
                .Include(p => p.Brands)
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product == null) return NotFound();
            return Ok(product);
        }

        // POST: api/Products
        [HttpPost]
        public async Task<ActionResult<Product>> SaveProduct(Product product)
        {
            if (string.IsNullOrEmpty(product.ImageUrl))
            {
                product.ImageUrl = "noimage.png";
            }
            product.Brands = product.Brands ?? new List<Brand>();
            
            _db.Products.Add(product);
            await _db.SaveChangesAsync();

            foreach (var brand in product.Brands)
            {
                brand.Product = null;
            }

            return CreatedAtAction(nameof(GetProduct), new { id = product.ProductId }, product);
        }

        // POST: api/Products/Upload/5
        [HttpPost("Upload/{id}")]
        public async Task<ActionResult<UploadResponse>> Upload(int id, IFormFile file)
        {
            var product = await _db.Products.FindAsync(id);
            if (product == null) return NotFound();

            if (file == null || file.Length == 0) return BadRequest("No file uploaded");

            string[] allowedTypes = { "image/jpeg", "image/png", "image/webp", "image/gif" };
            if (!allowedTypes.Contains(file.ContentType.ToLower()))
            {
                return BadRequest("Only JPEG, PNG, WEBP, and GIF files are allowed");
            }

            if (file.Length > 2 * 1024 * 1024)
            {
                return BadRequest("File size must be less than 2MB");
            }

            string ext = Path.GetExtension(file.FileName);
            string fileName = Path.GetFileNameWithoutExtension(Path.GetRandomFileName()) + ext;

            string folder = Path.Combine(_env.WebRootPath, "images");
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            string savePath = Path.Combine(folder, fileName);

            try
            {
                using (var fileStream = new FileStream(savePath, FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                }

                // puraton image delete korbe
                DeleteImageFile(product.ImageUrl);

                product.ImageUrl = fileName;
                await _db.SaveChangesAsync();

                return Ok(new UploadResponse { FileName = product.ImageUrl });
            }
            catch (IOException ex)
            {
                Console.WriteLine($"File Upload Exception: {ex.Message}");
                return StatusCode(500, "Error uploading file");
            }
        }

        // Standalone upload endpoint: POST api/Products/Upload
        [HttpPost("Upload")]
        public async Task<ActionResult<UploadResponse>> UploadFile(IFormFile file)
        {
            if (file == null || file.Length == 0) return BadRequest("No file uploaded");

            string[] allowedTypes = { "image/jpeg", "image/png", "image/webp", "image/gif" };
            if (!allowedTypes.Contains(file.ContentType.ToLower()))
            {
                return BadRequest("Only image files are allowed");
            }

            if (file.Length > 2 * 1024 * 1024)
            {
                return BadRequest("File size must be less than 2MB");
            }

            string ext = Path.GetExtension(file.FileName);
            string fileName = Path.GetFileNameWithoutExtension(Path.GetRandomFileName()) + ext;

            string folder = Path.Combine(_env.WebRootPath, "images");
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            string savePath = Path.Combine(folder, fileName);

            using (var fileStream = new FileStream(savePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            return Ok(new UploadResponse { FileName = fileName });
        }

        // PUT: api/Products/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProduct(int id, Product product)
        {
            if (id != product.ProductId)
            {
                return BadRequest();
            }

            try
            {
                var existingProduct = await _db.Products
                    .Include(p => p.Brands)
                    .FirstOrDefaultAsync(p => p.ProductId == id);

                if (existingProduct == null) return NotFound();

                // puraton image delete hbe jodi url khuje na pay
                if (!string.IsNullOrEmpty(product.ImageUrl) && product.ImageUrl != existingProduct.ImageUrl)
                {
                    DeleteImageFile(existingProduct.ImageUrl);
                }

                // updated value ke nie ashbe
                _db.Entry(existingProduct).CurrentValues.SetValues(product);

                if (product.Brands != null)
                {
                    // puraton brands gula ke khujbe
                    var brandsToRemove = existingProduct.Brands
                        .Where(existingBrand => !product.Brands.Any(updateBrand => updateBrand.BrandId == existingBrand.BrandId))
                        .ToList();

                    _db.Brands.RemoveRange(brandsToRemove);

                    foreach (var updateBrand in product.Brands)
                    {
                        var existingBrand = existingProduct.Brands.FirstOrDefault(b => b.BrandId == updateBrand.BrandId);
                        if (existingBrand != null)
                        {
                            _db.Entry(existingBrand).CurrentValues.SetValues(updateBrand);
                            _db.Entry(existingBrand).State = EntityState.Modified;
                        }
                        else
                        {
                            existingProduct.Brands.Add(updateBrand);
                        }
                    }

                    await _db.SaveChangesAsync();

                    foreach (var brand in existingProduct.Brands)
                    {
                        brand.Product = null;
                    }
                }
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ProductExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // DELETE: api/Products/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProducts(int id)
        {
            var product = await _db.Products.FindAsync(id);
            if (product == null) return NotFound();

            DeleteImageFile(product.ImageUrl);

            _db.Products.Remove(product);
            await _db.SaveChangesAsync();
            return NoContent();
        }

        private bool ProductExists(int id)
        {
            return _db.Products.Any(e => e.ProductId == id);
        }

        private void DeleteImageFile(string? fileName)
        {
            if (string.IsNullOrEmpty(fileName) || fileName == "noimage.png") return;
            string path = Path.Combine(_env.WebRootPath, "images", fileName);
            if (System.IO.File.Exists(path))
            {
                try
                {
                    System.IO.File.Delete(path);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error deleting image file: {ex.Message}");
                }
            }
        }
    }
}