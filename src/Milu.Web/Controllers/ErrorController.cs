using System.Diagnostics;
using Milu.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Milu.Web.Controllers;

[AllowAnonymous]
[Route("error")]
public sealed class ErrorController : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        return View("~/Views/Shared/Error.cshtml", new ErrorViewModel
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
        });
    }
}
