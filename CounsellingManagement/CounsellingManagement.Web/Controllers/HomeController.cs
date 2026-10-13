using CounsellingManagement.Web.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace CounsellingManagement.Web.Controllers
{
    /// <summary>Serves the public home, privacy, and error views.</summary>
    public class HomeController : Controller
    {
        /// <summary>Returns the public landing page.</summary>
        public IActionResult Index()
        {
            return View();
        }

        /// <summary>Returns the privacy information page.</summary>
        public IActionResult Privacy()
        {
            return View();
        }

        /// <summary>Builds an error view model using the current request ID.</summary>
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
