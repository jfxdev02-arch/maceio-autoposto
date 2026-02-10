using Microsoft.AspNetCore.Mvc;

namespace MaceioWeb.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
