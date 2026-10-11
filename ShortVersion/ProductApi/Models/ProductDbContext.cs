using System;
using Microsoft.EntityFrameworkCore;

namespace ProductApi.Models
{
    public partial class ProductDbContext : DbContext
    {
        public ProductDbContext(DbContextOptions<ProductDbContext> options)
            : base(options)
        {
        }

        public virtual DbSet<Category> Categories { get; set; } = null!;
        public virtual DbSet<Product> Products { get; set; } = null!;
        public virtual DbSet<Brand> Brands { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Category>(entity =>
            {
                entity.HasKey(e => e.CategoryId);
                entity.Property(e => e.CategoryName).HasMaxLength(100);
            });

            modelBuilder.Entity<Product>(entity =>
            {
                entity.HasKey(e => e.ProductId);
                entity.Property(e => e.ProductName).HasMaxLength(100);
                entity.Property(e => e.SerialNo).HasMaxLength(50);
                entity.Property(e => e.ImageUrl).HasMaxLength(200);
                entity.Property(e => e.Price).HasColumnType("decimal(18, 2)");
                entity.Property(e => e.ManufacturingDate).HasColumnType("datetime");

                entity.HasOne(d => d.Category)
                    .WithMany(p => p.Products)
                    .HasForeignKey(d => d.CategoryId)
                    .OnDelete(DeleteBehavior.ClientSetNull);
            });

            modelBuilder.Entity<Brand>(entity =>
            {
                entity.HasKey(e => e.BrandId);
                entity.Property(e => e.BrandName).HasMaxLength(100);

                entity.HasOne(d => d.Product)
                    .WithMany(p => p.Brands)
                    .HasForeignKey(d => d.ProductId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Seed Data
            modelBuilder.Entity<Category>().HasData(
                new Category { CategoryId = 1, CategoryName = "Electronics" },
                new Category { CategoryId = 2, CategoryName = "Clothing" },
                new Category { CategoryId = 3, CategoryName = "Food" }
            );

            modelBuilder.Entity<Product>().HasData(
                new Product
                {
                    ProductId = 1,
                    ProductName = "Gaming Laptop",
                    ManufacturingDate = new DateTime(2025, 6, 15),
                    SerialNo = "GL-2025-001",
                    ImageUrl = "noimage.png",
                    IsAvailable = true,
                    CategoryId = 1,
                    Price = 1499.99m
                },
                new Product
                {
                    ProductId = 2,
                    ProductName = "Wireless Mouse",
                    ManufacturingDate = new DateTime(2025, 5, 10),
                    SerialNo = "WM-2025-002",
                    ImageUrl = "noimage.png",
                    IsAvailable = true,
                    CategoryId = 1,
                    Price = 29.99m
                }
            );

            modelBuilder.Entity<Brand>().HasData(
                new Brand { BrandId = 1, BrandName = "Asus", Stock = 12, ProductId = 1 },
                new Brand { BrandId = 2, BrandName = "Lenovo", Stock = 7, ProductId = 1 },
                new Brand { BrandId = 3, BrandName = "Logitech", Stock = 25, ProductId = 2 }
            );
        }
    }
}
