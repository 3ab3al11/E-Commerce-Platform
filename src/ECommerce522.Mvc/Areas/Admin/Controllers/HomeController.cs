using Microsoft.AspNetCore.Mvc;

namespace ECommerce522.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class HomeController : Controller
    {
        public ViewResult Index()
        {
            return View();
        }

        public IActionResult NotFoundPage() 
        { 
            return View(); 
        }
    }
}
