using InventoryAPIProject.Controllers;
using InventoryAPIProject.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace InventoryAPI.Tests
{
    public class BrandsControllerTests
    {
        [Fact]
        public async Task GetBrands_ReturnsAllBrands()
        {
            using var db = TestDbContextFactory.CreateInMemoryDbContext(Guid.NewGuid().ToString());
            db.Brands.AddRange(
                new Brand { BrandId = 1, BrandName = "Apple" },
                new Brand { BrandId = 2, BrandName = "Samsung" }
            );
            await db.SaveChangesAsync();

            var controller = new BrandsController(db);
            var result = await controller.GetBrands();

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var items = Assert.IsAssignableFrom<IEnumerable<Brand>>(okResult.Value);
            Assert.Equal(2, items.Count());
        }

        [Fact]
        public async Task GetBrand_ReturnsBrand_WhenIdExists()
        {
            using var db = TestDbContextFactory.CreateInMemoryDbContext(Guid.NewGuid().ToString());
            db.Brands.Add(new Brand { BrandId = 3, BrandName = "Sony" });
            await db.SaveChangesAsync();

            var controller = new BrandsController(db);
            var result = await controller.GetBrand(3);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var brand = Assert.IsType<Brand>(okResult.Value);
            Assert.Equal("Sony", brand.BrandName);
        }

        [Fact]
        public async Task GetBrand_ReturnsNotFound_WhenIdDoesNotExist()
        {
            using var db = TestDbContextFactory.CreateInMemoryDbContext(Guid.NewGuid().ToString());
            var controller = new BrandsController(db);

            var result = await controller.GetBrand(888);

            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task PostBrand_AddsBrand_AndReturnsCreatedAtAction()
        {
            using var db = TestDbContextFactory.CreateInMemoryDbContext(Guid.NewGuid().ToString());
            var controller = new BrandsController(db);
            var newBrand = new Brand { BrandName = "Logitech" };

            var result = await controller.PostBrand(newBrand);

            var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var returnedBrand = Assert.IsType<Brand>(createdResult.Value);
            Assert.Equal("Logitech", returnedBrand.BrandName);
            Assert.Equal(1, db.Brands.Count());
        }

        [Fact]
        public async Task PutBrand_UpdatesBrand_WhenIdMatches()
        {
            using var db = TestDbContextFactory.CreateInMemoryDbContext(Guid.NewGuid().ToString());
            var brand = new Brand { BrandId = 10, BrandName = "Dell" };
            db.Brands.Add(brand);
            await db.SaveChangesAsync();

            var controller = new BrandsController(db);
            brand.BrandName = "Dell Technologies";

            var result = await controller.PutBrand(10, brand);

            Assert.IsType<NoContentResult>(result);
            var updated = await db.Brands.FindAsync(10);
            Assert.Equal("Dell Technologies", updated?.BrandName);
        }

        [Fact]
        public async Task DeleteBrand_RemovesBrand_WhenIdExists()
        {
            using var db = TestDbContextFactory.CreateInMemoryDbContext(Guid.NewGuid().ToString());
            var brand = new Brand { BrandId = 12, BrandName = "ToDiscard" };
            db.Brands.Add(brand);
            await db.SaveChangesAsync();

            var controller = new BrandsController(db);
            var result = await controller.DeleteBrand(12);

            Assert.IsType<NoContentResult>(result);
            Assert.Null(await db.Brands.FindAsync(12));
        }
    }
}
