using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CurioBid.Controllers
{
    public class ReviewerController : Controller
    {
        [Authorize(Roles = "Reviewer")]
        public IActionResult Index()
        {
            return View();
        }
    }
}