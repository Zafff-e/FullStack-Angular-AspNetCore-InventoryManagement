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
    public class CategoriesControllerTests
    {
        [Fact]
        public async Task GetCategories_ReturnsAllCategories()
        {
            using var db = TestDbContextFactory.CreateInMemoryDbContext(Guid.NewGuid().ToString());
            db.Categories.AddRange(
                new Category { CategoryId = 1, CategoryName = "Electronics" },
                new Category { CategoryId = 2, CategoryName = "Appliances" }
            );
            await db.SaveChangesAsync();

            var controller = new CategoriesController(db);
            var result = await controller.GetCategories();

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var items = Assert.IsAssignableFrom<IEnumerable<Category>>(okResult.Value);
            Assert.Equal(2, items.Count());
        }

        [Fact]
        public async Task GetCategory_ReturnsCategory_WhenIdExists()
        {
            using var db = TestDbContextFactory.CreateInMemoryDbContext(Guid.NewGuid().ToString());
            db.Categories.Add(new Category { CategoryId = 10, CategoryName = "Gaming" });
            await db.SaveChangesAsync();

            var controller = new CategoriesController(db);
            var result = await controller.GetCategory(10);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var category = Assert.IsType<Category>(okResult.Value);
            Assert.Equal("Gaming", category.CategoryName);
        }

        [Fact]
        public async Task GetCategory_ReturnsNotFound_WhenIdDoesNotExist()
        {
            using var db = TestDbContextFactory.CreateInMemoryDbContext(Guid.NewGuid().ToString());
            var controller = new CategoriesController(db);

            var result = await controller.GetCategory(999);

            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task PostCategory_AddsCategory_AndReturnsCreatedAtAction()
        {
            using var db = TestDbContextFactory.CreateInMemoryDbContext(Guid.NewGuid().ToString());
            var controller = new CategoriesController(db);
            var newCat = new Category { CategoryName = "Office Supplies" };

            var result = await controller.PostCategory(newCat);

            var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var returnedCat = Assert.IsType<Category>(createdResult.Value);
            Assert.Equal("Office Supplies", returnedCat.CategoryName);
            Assert.Equal(1, db.Categories.Count());
        }

        [Fact]
        public async Task PutCategory_UpdatesCategory_WhenIdMatches()
        {
            using var db = TestDbContextFactory.CreateInMemoryDbContext(Guid.NewGuid().ToString());
            var cat = new Category { CategoryId = 5, CategoryName = "Original" };
            db.Categories.Add(cat);
            await db.SaveChangesAsync();

            var controller = new CategoriesController(db);
            cat.CategoryName = "Updated Name";

            var result = await controller.PutCategory(5, cat);

            Assert.IsType<NoContentResult>(result);
            var updated = await db.Categories.FindAsync(5);
            Assert.Equal("Updated Name", updated?.CategoryName);
        }

        [Fact]
        public async Task PutCategory_ReturnsBadRequest_WhenIdMismatches()
        {
            using var db = TestDbContextFactory.CreateInMemoryDbContext(Guid.NewGuid().ToString());
            var controller = new CategoriesController(db);
            var cat = new Category { CategoryId = 1, CategoryName = "Mismatch" };

            var result = await controller.PutCategory(99, cat);

            Assert.IsType<BadRequestResult>(result);
        }

        [Fact]
        public async Task DeleteCategory_RemovesCategory_WhenIdExists()
        {
            using var db = TestDbContextFactory.CreateInMemoryDbContext(Guid.NewGuid().ToString());
            var cat = new Category { CategoryId = 7, CategoryName = "ToDelete" };
            db.Categories.Add(cat);
            await db.SaveChangesAsync();

            var controller = new CategoriesController(db);
            var result = await controller.DeleteCategory(7);

            Assert.IsType<NoContentResult>(result);
            Assert.Null(await db.Categories.FindAsync(7));
        }
    }
}
