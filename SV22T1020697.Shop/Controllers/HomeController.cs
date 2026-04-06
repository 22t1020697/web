using Microsoft.AspNetCore.Mvc;
using SV22T1020697.BusinessLayers;
using SV22T1020697.Models.Catalog; // ??m b?o có namespace này cho l?p Product
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
            // 1. L?y danh sách s?n ph?m m?i nh?t (gi? s? l?y 8 s?n ph?m ??u tiên)
            var inputNew = new ProductSearchInput()
            {
                Page = 1,
                PageSize = 4,
                SearchValue = "" // B?n có th? thêm tiêu chí l?c n?u c?n
            };
            var resultNew = await CatalogDataService.ListProductsAsync(inputNew);
            ViewBag.NewProducts = resultNew.DataItems;

            // 2. L?y danh sách s?n ph?m bán ch?y 
            // N?u DB ch?a có hàm TopSellers, b?n có th? l?y ng?u nhiên ho?c theo tiêu chí khác
            var inputBest = new ProductSearchInput()
            {
                Page = 2, // L?y trang 2 ?? d? li?u khác ?i m?t chút cho demo
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
