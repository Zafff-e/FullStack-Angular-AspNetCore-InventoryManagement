using Microsoft.EntityFrameworkCore;
using InventoryAPIProject.Controllers;
using InventoryAPIProject.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace InventoryAPI.Tests
{
    public class ProductsControllerTests
    {
        private Mock<IWebHostEnvironment> CreateMockEnv()
        {
            var mockEnv = new Mock<IWebHostEnvironment>();
            var tempPath = Path.Combine(Path.GetTempPath(), "InventoryApiTests_" + Guid.NewGuid().ToString());
            Directory.CreateDirectory(Path.Combine(tempPath, "images"));
            mockEnv.Setup(e => e.WebRootPath).Returns(tempPath);
            return mockEnv;
        }

        [Fact]
        public async Task GetProducts_ReturnsAllProducts()
        {
            string dbName = Guid.NewGuid().ToString();
            using var db = TestDbContextFactory.CreateInMemoryDbContext(dbName);
            db.Products.AddRange(
                new Product { ProductId = 1, ProductName = "MacBook Pro", SerialNo = "SN-001", Price = 2000, CategoryId = 1, ImageUrl = "macbook.png" },
                new Product { ProductId = 2, ProductName = "ThinkPad X1", SerialNo = "SN-002", Price = 1800, CategoryId = 1, ImageUrl = "thinkpad.png" }
            );
            await db.SaveChangesAsync();

            var mockEnv = CreateMockEnv();
            var controller = new ProductsController(db, mockEnv.Object);

            var result = await controller.GetProducts();

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var items = Assert.IsAssignableFrom<IEnumerable<Product>>(okResult.Value);
            Assert.Equal(2, items.Count());
        }

        [Fact]
        public async Task GetProduct_ReturnsProduct_WhenIdExists()
        {
            using var db = TestDbContextFactory.CreateInMemoryDbContext(Guid.NewGuid().ToString());
            db.Products.Add(new Product { ProductId = 5, ProductName = "iPad Air", SerialNo = "SN-005", Price = 800, CategoryId = 1, ImageUrl = "ipad.png" });
            await db.SaveChangesAsync();

            var mockEnv = CreateMockEnv();
            var controller = new ProductsController(db, mockEnv.Object);

            var result = await controller.GetProduct(5);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var product = Assert.IsType<Product>(okResult.Value);
            Assert.Equal("iPad Air", product.ProductName);
        }

        [Fact]
        public async Task GetProductWithBrands_IncludesCategoryAndBrands()
        {
            using var db = TestDbContextFactory.CreateInMemoryDbContext(Guid.NewGuid().ToString());
            var cat = new Category { CategoryId = 1, CategoryName = "Laptops" };
            var product = new Product
            {
                ProductId = 10,
                ProductName = "Surface Laptop",
                SerialNo = "SN-SURF",
                Price = 1500,
                Category = cat,
                ImageUrl = "surface.png",
                Brands = new List<Brand>
                {
                    new Brand { BrandId = 1, BrandName = "Microsoft" }
                }
            };
            db.Products.Add(product);
            await db.SaveChangesAsync();

            var mockEnv = CreateMockEnv();
            var controller = new ProductsController(db, mockEnv.Object);

            var result = await controller.GetProductWithBrands(10);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var fetched = Assert.IsType<Product>(okResult.Value);
            Assert.Equal("Surface Laptop", fetched.ProductName);
            Assert.NotNull(fetched.Category);
            Assert.Equal("Laptops", fetched.Category.CategoryName);
            Assert.Single(fetched.Brands);
        }

        [Fact]
        public async Task SaveProduct_AssignsDefaultImage_WhenImageUrlIsEmpty()
        {
            using var db = TestDbContextFactory.CreateInMemoryDbContext(Guid.NewGuid().ToString());
            var mockEnv = CreateMockEnv();
            var controller = new ProductsController(db, mockEnv.Object);

            var newProd = new Product
            {
                ProductName = "Galaxy Tab",
                SerialNo = "SN-TAB",
                Price = 600,
                CategoryId = 1,
                ImageUrl = ""
            };

            var result = await controller.SaveProduct(newProd);

            var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var created = Assert.IsType<Product>(createdResult.Value);
            Assert.Equal("noimage.png", created.ImageUrl);
            Assert.Equal(1, db.Products.Count());
        }

        [Fact]
        public async Task UpdateProduct_SyncsBrands_RemovingDeletedAndModifyingExisting()
        {
            string dbName = Guid.NewGuid().ToString();
            using var db = TestDbContextFactory.CreateInMemoryDbContext(dbName);
            var prod = new Product
            {
                ProductId = 20,
                ProductName = "Multi Brand Device",
                SerialNo = "SN-MULTI",
                Price = 1000,
                CategoryId = 1,
                ImageUrl = "multi.png",
                Brands = new List<Brand>
                {
                    new Brand { BrandId = 101, BrandName = "Brand A" },
                    new Brand { BrandId = 102, BrandName = "Brand B" }
                }
            };
            db.Products.Add(prod);
            await db.SaveChangesAsync();

            var mockEnv = CreateMockEnv();
            var controller = new ProductsController(db, mockEnv.Object);

            // Update: Remove Brand B, modify Brand A, add Brand C
            var updatedProd = new Product
            {
                ProductId = 20,
                ProductName = "Multi Brand Device Updated",
                SerialNo = "SN-MULTI-2",
                Price = 1200,
                CategoryId = 1,
                ImageUrl = "multi.png",
                Brands = new List<Brand>
                {
                    new Brand { BrandId = 101, BrandName = "Brand A Renamed" },
                    new Brand { BrandId = 0, BrandName = "Brand C", ProductId = 20 }
                }
            };

            var result = await controller.UpdateProduct(20, updatedProd);

            Assert.IsType<NoContentResult>(result);

            // Create a fresh DbContext with the same database name to query actual persisted state without in-memory tracker cache
            using var queryDb = TestDbContextFactory.CreateInMemoryDbContext(dbName);
            var reloaded = await queryDb.Products.Include(p => p.Brands).FirstOrDefaultAsync(p => p.ProductId == 20);
            Assert.NotNull(reloaded);
            Assert.Equal("Multi Brand Device Updated", reloaded.ProductName);
            Assert.Equal(2, reloaded.Brands.Count);
            Assert.Contains(reloaded.Brands, b => b.BrandName == "Brand A Renamed");
            Assert.Contains(reloaded.Brands, b => b.BrandName == "Brand C");
            Assert.DoesNotContain(reloaded.Brands, b => b.BrandName == "Brand B");
        }

        [Fact]
        public async Task DeleteProducts_RemovesProductFromDatabase()
        {
            using var db = TestDbContextFactory.CreateInMemoryDbContext(Guid.NewGuid().ToString());
            var prod = new Product { ProductId = 30, ProductName = "Old Monitor", SerialNo = "SN-MON", Price = 200, CategoryId = 1, ImageUrl = "monitor.png" };
            db.Products.Add(prod);
            await db.SaveChangesAsync();

            var mockEnv = CreateMockEnv();
            var controller = new ProductsController(db, mockEnv.Object);

            var result = await controller.DeleteProducts(30);

            Assert.IsType<NoContentResult>(result);
            Assert.Null(await db.Products.FindAsync(30));
        }
    }
}
