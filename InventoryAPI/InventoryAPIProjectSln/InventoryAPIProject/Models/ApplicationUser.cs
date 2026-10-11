using Microsoft.AspNetCore.Identity;

namespace InventoryAPIProject.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string? FullName { get; set; }
    }
}
