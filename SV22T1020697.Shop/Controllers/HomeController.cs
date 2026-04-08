using Microsoft.AspNetCore.Mvc;
using SV22T1020697.BusinessLayers;
using SV22T1020697.Models.Catalog; 
using SV22T1020697.Models.Common;
using SV22T1020697.Shop.Models;
using System.Diagnostics;

namespace SV22T1020697.Shop.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            
            var inputNew = new ProductSearchInput()
            {
                Page = 1,
                PageSize = 4,
                SearchValue = "" 
            };
            var resultNew = await CatalogDataService.ListProductsAsync(inputNew);
            ViewBag.NewProducts = resultNew.DataItems;

            // lấy ds sp bán chạy 
           
            var inputBest = new ProductSearchInput()
            {
                Page = 2, 
                PageSize = 4,
                SearchValue = ""
            };
            var resultBest = await CatalogDataService.ListProductsAsync(inputBest);
            ViewBag.BestSellers = resultBest.DataItems;

            return View();
        }



        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
