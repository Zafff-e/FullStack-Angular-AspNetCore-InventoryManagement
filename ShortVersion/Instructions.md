Simple Product Web API (for an Angular client)
Database First · EF Core · Controllers · Swagger · CORS · Image upload No stored procedures, no JWT, no aggregates.

Design choices that make Angular easy
One DTO for everything. The same ProductDto shape is used to read, create and update, so Angular needs just one Product interface.
Plain JSON for create and update. Brands are a normal JSON array inside the product, so an Angular FormArray maps straight onto it.
Image upload is a separate call. Angular uploads the file first, gets back a file name, and puts that name in the product's imageUrl. The product save stays pure JSON, with no FormData and no Brands[0].BrandName keys.
Dates are DateOnly. They arrive as "2025-06-15", which binds directly to <input type="date"> with no conversion.
No service layer. Controllers use the DbContext directly, like your teacher's MVC project.
Endpoints
Method	URL	Body	Returns
GET	/api/categories	–	[{ categoryId, categoryName }]
GET	/api/products	–	ProductDto[]
GET	/api/products/{id}	–	ProductDto or 404
POST	/api/products	ProductDto (JSON)	201 + ProductDto
PUT	/api/products/{id}	ProductDto (JSON)	204
DELETE	/api/products/{id}	–	204
POST	/api/products/upload	file (multipart)	{ fileName }
Images are served at https://localhost:<port>/images/<fileName>.

STEP 1 — Create the database
SQL Server Object Explorer → (localdb)\MSSQLLocalDB → New Query:

CREATE DATABASE ProductDB;
GO
USE ProductDB;
GO

CREATE TABLE Categories (
    CategoryId INT IDENTITY(1,1) PRIMARY KEY,
    CategoryName NVARCHAR(100) NOT NULL
);

CREATE TABLE Products (
    ProductId INT IDENTITY(1,1) PRIMARY KEY,
    ProductName NVARCHAR(100) NOT NULL,
    ManufacturingDate DATETIME NOT NULL,
    SerialNo NVARCHAR(50) NOT NULL,
    ImageUrl NVARCHAR(200) NOT NULL,
    IsAvailable BIT NOT NULL,
    CategoryId INT NOT NULL FOREIGN KEY REFERENCES Categories(CategoryId),
    Price DECIMAL(18,2) NOT NULL
);

CREATE TABLE Brands (
    BrandId INT IDENTITY(1,1) PRIMARY KEY,
    BrandName NVARCHAR(100) NOT NULL,
    Stock INT NOT NULL,
    ProductId INT NOT NULL FOREIGN KEY REFERENCES Products(ProductId) ON DELETE CASCADE
);

INSERT INTO Categories (CategoryName) VALUES ('Electronics'), ('Clothing'), ('Food');
STEP 2 — Create the project
Visual Studio → New Project → ASP.NET Core Web API.
Name: ProductApi. Framework: .NET 9 (or .NET 8).
Authentication: None. Tick Configure for HTTPS and Use controllers.
Delete WeatherForecast.cs and Controllers/WeatherForecastController.cs.
Create the folders Dtos and wwwroot/images. Put a small image in wwwroot/images named noimage.png.
Install the packages (Tools → NuGet Package Manager → Manage NuGet Packages):

Microsoft.EntityFrameworkCore.SqlServer
Microsoft.EntityFrameworkCore.Tools
Microsoft.EntityFrameworkCore.Design
Swashbuckle.AspNetCore
Use the same major version (8.x or 9.x) as your project's framework for the three EF packages.

STEP 3 — appsettings.json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "con": "Server=(LocalDB)\\MSSQLLocalDB;Database=ProductDB;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True"
  }
}
STEP 4 — Scaffold the database into models
Package Manager Console:

Scaffold-DbContext "Name=ConnectionStrings:con" Microsoft.EntityFrameworkCore.SqlServer -OutputDir Models -Context ProductDbContext -NoOnConfiguring -Force
This generates Models/Category.cs, Models/Product.cs, Models/Brand.cs and Models/ProductDbContext.cs. Verify Product.cs looks like this:

namespace ProductApi.Models;

public partial class Product
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = null!;
    public DateTime ManufacturingDate { get; set; }
    public string SerialNo { get; set; } = null!;
    public string ImageUrl { get; set; } = null!;
    public bool IsAvailable { get; set; }
    public int CategoryId { get; set; }
    public decimal Price { get; set; }
    public virtual Category Category { get; set; } = null!;
    public virtual ICollection<Brand> Brands { get; set; } = new List<Brand>();
}
STEP 5 — DTOs
Dtos/ProductDto.cs

using System.ComponentModel.DataAnnotations;

namespace ProductApi.Dtos
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

        public string? CategoryName { get; set; }   // filled on GET, ignored on POST/PUT

        [Range(0.01, 1000000, ErrorMessage = "Price must be greater than 0")]
        public decimal Price { get; set; }

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
Why a DTO instead of the entity? Returning Product directly would loop forever when serialized (Product → Category → Products → Category …). It would also force Angular to send nested objects it doesn't need.

STEP 6 — Program.cs
Replace the whole file:

using Microsoft.EntityFrameworkCore;
using ProductApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddDbContext<ProductDbContext>(op =>
    op.UseSqlServer(builder.Configuration.GetConnectionString("con")));

// Allow the Angular dev server to call this API
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseStaticFiles();        // serves wwwroot/images
app.UseCors("AllowAngular");
app.UseAuthorization();
app.MapControllers();

app.Run();
To open Swagger automatically on F5: in Properties/launchSettings.json, set "launchBrowser": true and "launchUrl": "swagger" in the https profile.

STEP 7 — CategoriesController
Controllers/CategoriesController.cs

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductApi.Models;

namespace ProductApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoriesController : ControllerBase
    {
        private readonly ProductDbContext _db;

        public CategoriesController(ProductDbContext db)
        {
            _db = db;
        }

        // GET: api/categories   (for the dropdown)
        [HttpGet]
        public async Task<IActionResult> GetCategories()
        {
            var categories = await _db.Categories
                .Select(c => new { c.CategoryId, c.CategoryName })
                .ToListAsync();
            return Ok(categories);
        }
    }
}
STEP 8 — ProductsController
Controllers/ProductsController.cs

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductApi.Dtos;
using ProductApi.Models;

namespace ProductApi.Controllers
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

        // GET: api/products
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProductDto>>> GetProducts()
        {
            var products = await _db.Products
                .Include(p => p.Category)
                .Include(p => p.Brands)
                .ToListAsync();

            return Ok(products.Select(ToDto));
        }

        // GET: api/products/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ProductDto>> GetProduct(int id)
        {
            var product = await _db.Products
                .Include(p => p.Category)
                .Include(p => p.Brands)
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product == null) return NotFound();
            return Ok(ToDto(product));
        }

        // POST: api/products
        [HttpPost]
        public async Task<ActionResult<ProductDto>> PostProduct(ProductDto dto)
        {
            if (!await _db.Categories.AnyAsync(c => c.CategoryId == dto.CategoryId))
                return BadRequest("Invalid category");

            var product = new Product
            {
                ProductName = dto.ProductName,
                ManufacturingDate = dto.ManufacturingDate.ToDateTime(TimeOnly.MinValue),
                SerialNo = dto.SerialNo,
                ImageUrl = string.IsNullOrEmpty(dto.ImageUrl) ? "noimage.png" : dto.ImageUrl,
                IsAvailable = dto.IsAvailable,
                CategoryId = dto.CategoryId,
                Price = dto.Price,
                Brands = dto.Brands.Select(b => new Brand
                {
                    BrandName = b.BrandName,
                    Stock = b.Stock
                }).ToList()
            };

            _db.Products.Add(product);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetProduct), new { id = product.ProductId }, ToDto(product));
        }

        // PUT: api/products/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutProduct(int id, ProductDto dto)
        {
            if (id != dto.ProductId) return BadRequest("Id mismatch");

            var product = await _db.Products
                .Include(p => p.Brands)
                .FirstOrDefaultAsync(p => p.ProductId == id);
            if (product == null) return NotFound();

            if (!await _db.Categories.AnyAsync(c => c.CategoryId == dto.CategoryId))
                return BadRequest("Invalid category");

            // If a new image was uploaded, delete the old file
            if (!string.IsNullOrEmpty(dto.ImageUrl) && dto.ImageUrl != product.ImageUrl)
            {
                DeleteImage(product.ImageUrl);
                product.ImageUrl = dto.ImageUrl;
            }

            product.ProductName = dto.ProductName;
            product.ManufacturingDate = dto.ManufacturingDate.ToDateTime(TimeOnly.MinValue);
            product.SerialNo = dto.SerialNo;
            product.IsAvailable = dto.IsAvailable;
            product.CategoryId = dto.CategoryId;
            product.Price = dto.Price;

            // Replace all brands: remove old ones, add the ones sent from Angular
            _db.Brands.RemoveRange(product.Brands);
            foreach (var b in dto.Brands)
            {
                _db.Brands.Add(new Brand
                {
                    BrandName = b.BrandName,
                    Stock = b.Stock,
                    ProductId = product.ProductId
                });
            }

            await _db.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/products/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _db.Products.FindAsync(id);
            if (product == null) return NotFound();

            DeleteImage(product.ImageUrl);
            _db.Products.Remove(product);      // brands deleted by ON DELETE CASCADE
            await _db.SaveChangesAsync();
            return NoContent();
        }

        // POST: api/products/upload   (multipart/form-data, field name "file")
        [HttpPost("upload")]
        public async Task<IActionResult> UploadImage(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded");

            string[] allowed = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            var extension = Path.GetExtension(file.FileName).ToLower();
            if (!allowed.Contains(extension))
                return BadRequest("Only image files are allowed");

            var fileName = Guid.NewGuid().ToString() + extension;
            var folder = Path.Combine(_env.WebRootPath, "images");
            Directory.CreateDirectory(folder);

            using (var stream = new FileStream(Path.Combine(folder, fileName), FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return Ok(new { fileName });
        }

        // ---------- helpers ----------
        private static ProductDto ToDto(Product p)
        {
            return new ProductDto
            {
                ProductId = p.ProductId,
                ProductName = p.ProductName,
                ManufacturingDate = DateOnly.FromDateTime(p.ManufacturingDate),
                SerialNo = p.SerialNo,
                ImageUrl = p.ImageUrl,
                IsAvailable = p.IsAvailable,
                CategoryId = p.CategoryId,
                CategoryName = p.Category?.CategoryName,
                Price = p.Price,
                Brands = p.Brands.Select(b => new BrandDto
                {
                    BrandId = b.BrandId,
                    BrandName = b.BrandName,
                    Stock = b.Stock
                }).ToList()
            };
        }

        private void DeleteImage(string? fileName)
        {
            if (string.IsNullOrEmpty(fileName) || fileName == "noimage.png") return;
            var path = Path.Combine(_env.WebRootPath, "images", fileName);
            if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
        }
    }
}
Notes:

Automatic validation: [ApiController] checks the Data Annotations for you and returns 400 with the errors, so there's no ModelState.IsValid check.
System.IO.File: written out in full because ControllerBase has its own File() method, which would clash.
No [FromForm] on the upload's IFormFile parameter: [ApiController] infers it, and adding [FromForm] makes older Swashbuckle versions fail to generate the Swagger page.
STEP 9 — Test in Swagger
Press F5 and open https://localhost:<port>/swagger.

GET /api/categories returns 3 categories.
POST /api/products/upload: choose an image and copy the fileName from the response.
POST /api/products with this body (paste your file name in imageUrl):
{
  "productName": "Gaming Laptop",
  "manufacturingDate": "2025-06-15",
  "serialNo": "GL-2025-001",
  "imageUrl": "paste-file-name-here.png",
  "isAvailable": true,
  "categoryId": 1,
  "price": 1499.99,
  "brands": [
    { "brandName": "Asus", "stock": 12 },
    { "brandName": "Lenovo", "stock": 7 }
  ]
}
GET /api/products shows the product with its brands and category name.
Open https://localhost:<port>/images/<fileName> in the browser to see the image.
PUT /api/products/1 with the same body plus "productId": 1, changing a brand, returns 204.
DELETE /api/products/1 returns 204.
POST with an empty productName returns 400 with a validation error.
STEP 10 — What the Angular client will use
Interfaces (product.ts):

export interface Brand {
  brandId?: number;
  brandName: string;
  stock: number;
}

export interface Product {
  productId?: number;
  productName: string;
  manufacturingDate: string;   // "yyyy-MM-dd" → works directly with <input type="date">
  serialNo: string;
  imageUrl?: string;
  isAvailable: boolean;
  categoryId: number;
  categoryName?: string;
  price: number;
  brands: Brand[];
}

export interface Category {
  categoryId: number;
  categoryName: string;
}
Service (product.service.ts):

import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Product, Category } from './product';

@Injectable({ providedIn: 'root' })
export class ProductService {
  apiUrl = 'https://localhost:7001/api';      // change 7001 to your API's https port
  imageUrl = 'https://localhost:7001/images/';

  constructor(private http: HttpClient) {}

  getCategories() { return this.http.get<Category[]>(`${this.apiUrl}/categories`); }
  getProducts() { return this.http.get<Product[]>(`${this.apiUrl}/products`); }
  getProduct(id: number) { return this.http.get<Product>(`${this.apiUrl}/products/${id}`); }
  addProduct(p: Product) { return this.http.post<Product>(`${this.apiUrl}/products`, p); }
  updateProduct(id: number, p: Product) { return this.http.put(`${this.apiUrl}/products/${id}`, p); }
  deleteProduct(id: number) { return this.http.delete(`${this.apiUrl}/products/${id}`); }

  uploadImage(file: File) {
    const formData = new FormData();
    formData.append('file', file);    // name must be "file"
    return this.http.post<{ fileName: string }>(`${this.apiUrl}/products/upload`, formData);
  }
}
Image upload flow in the form component:

On file select, call uploadImage(file).
Put the returned fileName into the form's imageUrl control.
On Save, send the whole form value with addProduct or updateProduct.
Showing an image: <img [src]="service.imageUrl + p.imageUrl" width="80">.

Brands: use a FormArray of { brandName, stock } groups. Its value is already the brands array the API expects.

Troubleshooting
Problem	Fix
CORS error in the Angular console	Angular must run on http://localhost:4200 (the origin in WithOrigins), and app.UseCors must be in Program.cs
Angular request fails with a certificate error	Run dotnet dev-certs https --trust once, or open the API URL in the browser and accept the certificate
Image returns 404	wwwroot/images is missing, app.UseStaticFiles() is missing, or the file name is wrong
WebRootPath is null	The wwwroot folder doesn't exist
400 on POST from Angular	Look at the response's errors object; usually categoryId is 0 or productName is empty
Upload returns 400 "No file uploaded"	The FormData key isn't file
PUT returns "Id mismatch"	The body's productId must equal the id in the URL
JSON cycle error	You returned an entity instead of ProductDto