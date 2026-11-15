using Microsoft.AspNetCore.Mvc;

namespace CurioBid.Controllers
{
    public class SellerController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}