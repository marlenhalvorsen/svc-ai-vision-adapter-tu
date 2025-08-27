using Microsoft.AspNetCore.Mvc;

namespace svc_ai_vision_adapter.Web.Controllers
{
    public class recognitionController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
