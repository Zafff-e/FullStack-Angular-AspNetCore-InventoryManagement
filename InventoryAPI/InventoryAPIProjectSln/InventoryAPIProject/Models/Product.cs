using System;
using System.Collections.Generic;

namespace InventoryAPIProject.Models;

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

    public virtual ICollection<Brand> Brands { get; set; } = new List<Brand>();

    public virtual Category? Category { get; set; }
}
