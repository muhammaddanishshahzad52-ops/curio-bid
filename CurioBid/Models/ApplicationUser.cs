using Microsoft.AspNetCore.Identity;
namespace CurioBid.Models
{
    public class ApplicationUser:IdentityUser
    {
        public string FullName { get; set; } = string.Empty;
    }
}
