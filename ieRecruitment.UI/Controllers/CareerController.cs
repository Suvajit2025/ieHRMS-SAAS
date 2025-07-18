using Microsoft.AspNetCore.Mvc;

namespace ieRecruitment.UI.Controllers
{
    public class CareerController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult FromLanding()
        {
            return View();
        }
               
    }
}
