using InventoryAPIProject.Models;
using Microsoft.EntityFrameworkCore;
using System;

namespace InventoryAPI.Tests
{
    public static class TestDbContextFactory
    {
        public static ProductDbContext CreateInMemoryDbContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<ProductDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;

            return new ProductDbContext(options);
        }
    }
}
