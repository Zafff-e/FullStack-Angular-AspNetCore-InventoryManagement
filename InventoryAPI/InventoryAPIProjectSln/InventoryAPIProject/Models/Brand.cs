using System;
using System.Collections.Generic;

namespace InventoryAPIProject.Models;

public partial class Brand
{
    public int BrandId { get; set; }

    public string BrandName { get; set; } = null!;

    public int Stock { get; set; }

    public int ProductId { get; set; }

    public virtual Product? Product { get; set; }
}
