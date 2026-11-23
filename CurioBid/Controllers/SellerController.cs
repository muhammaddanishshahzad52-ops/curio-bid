using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CurioBid.Controllers
{
    [Authorize(Roles = "User")]
    public class SellerController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}