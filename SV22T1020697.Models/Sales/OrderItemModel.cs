using SV22T1020697.Models.Sales;

namespace SV22T1020697.Models.Sales
{
    public class OrderItemModel
    {
        public int OrderID { get; set; }

        public string CustomerName { get; set; } = "";
        public DateTime OrderTime { get; set; }
        public int Status { get; set; } // Giá trị số từ Database
        public decimal TotalAmount { get; set; }
        public string DeliveryAddress { get; set; } = "";
        public string DeliveryProvince { get; set; } = "";

        // Tận dụng Extension của bạn để lấy câu mô tả tiếng Việt
        public string StatusDescription => ((OrderStatusEnum)Status).GetDescription();
    }
}