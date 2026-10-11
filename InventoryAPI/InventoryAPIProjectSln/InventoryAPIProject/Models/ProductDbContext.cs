using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace InventoryAPIProject.Models;

public partial class ProductDbContext : IdentityDbContext<ApplicationUser>
{
    public ProductDbContext(DbContextOptions<ProductDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Brand> Brands { get; set; } = null!;

    public virtual DbSet<Category> Categories { get; set; } = null!;

    public virtual DbSet<Product> Products { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Brand>(entity =>
        {
            entity.HasKey(e => e.BrandId);

            entity.Property(e => e.BrandName).HasMaxLength(100);

            entity.HasOne(d => d.Product).WithMany(p => p.Brands)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.CategoryId);

            entity.Property(e => e.CategoryName).HasMaxLength(100);
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.ProductId);

            entity.Property(e => e.ImageUrl).HasMaxLength(200);
            entity.Property(e => e.ManufacturingDate).HasColumnType("datetime");
            entity.Property(e => e.Price).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ProductName).HasMaxLength(100);
            entity.Property(e => e.SerialNo).HasMaxLength(50);

            entity.HasOne(d => d.Category).WithMany(p => p.Products)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        // Seed Data
        modelBuilder.Entity<Category>().HasData(
            new Category { CategoryId = 1, CategoryName = "Electronics" },
            new Category { CategoryId = 2, CategoryName = "Computers" },
            new Category { CategoryId = 3, CategoryName = "Mobile Accessories" }
        );

        modelBuilder.Entity<Product>().HasData(
            new Product
            {
                ProductId = 1,
                ProductName = "Wireless Mouse",
                ManufacturingDate = new DateTime(2024, 1, 15),
                SerialNo = "WM-1001",
                ImageUrl = "1.jpg",
                IsAvailable = true,
                CategoryId = 1,
                Price = 25.50m
            },
            new Product
            {
                ProductId = 2,
                ProductName = "Mechanical Keyboard",
                ManufacturingDate = new DateTime(2024, 2, 20),
                SerialNo = "MK-2002",
                ImageUrl = "2.jpg",
                IsAvailable = true,
                CategoryId = 2,
                Price = 85.00m
            }
        );

        modelBuilder.Entity<Brand>().HasData(
            new Brand { BrandId = 1, BrandName = "Logitech", Stock = 50, ProductId = 1 },
            new Brand { BrandId = 2, BrandName = "Razer", Stock = 30, ProductId = 1 },
            new Brand { BrandId = 3, BrandName = "Corsair", Stock = 40, ProductId = 2 },
            new Brand { BrandId = 4, BrandName = "Keychron", Stock = 20, ProductId = 2 }
        );

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
