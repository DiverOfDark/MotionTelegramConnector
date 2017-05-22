using Microsoft.AspNetCore.Mvc;

namespace MotionTelegramConnector.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return Content("OK");
        }
    }
}