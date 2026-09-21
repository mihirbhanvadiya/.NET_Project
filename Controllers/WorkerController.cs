using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JobSequenceApp.Controllers
{
    [Authorize(Roles = "Worker")]
    public class WorkerController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
