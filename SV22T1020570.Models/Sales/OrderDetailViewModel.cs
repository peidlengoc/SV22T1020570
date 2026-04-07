namespace SV22T1020570.Models.Sales
{
    public class OrderDetailViewModel
    {
        public int OrderID { get; set; }
        public DateTime OrderTime { get; set; }
        
        public string DeliveryAddress { get; set; } = "";
        public string DeliveryProvince { get; set; } = "";
        public string ProductName { get; set; } = "";
        public OrderStatusEnum Status { get; set; }

        public List<OrderDetailViewInfo> Details { get; set; } = new();
        public string Photo { get; set; } = "";

        public decimal TotalPrice { get; set; }
        public DateTime? AcceptTime { get; set; }
        public DateTime? ShippedTime { get; set; }
        public DateTime? FinishedTime { get; set; }
    }
}