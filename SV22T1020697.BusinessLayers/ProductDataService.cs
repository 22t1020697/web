using SV22T1020697.DataLayers.Interfaces;
using SV22T1020697.DataLayers.SQLServer;
using SV22T1020697.Models.Catalog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SV22T1020697.BusinessLayers
{
    public static class ProductDataService
    {

        private static readonly IProductRepository productDB;

        static ProductDataService()
        {
            // Khởi tạo productDB với connection string từ Configuration
            productDB = new ProductRepository(Configuration.ConnectionString);
        }
        public static Product? GetProduct(int productID) // Phải có public
        {
            return productDB.Get(productID);
        }
    }
}
