using Microsoft.AspNetCore.Mvc;

namespace CurioBid.Controllers
{
    public class ReviewerController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}