using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SV22T1020570.Models.Sales
{
    public class ShoppingCartItem
    {
        public int CustomerID { get; set; }
        public int ProductID { get; set; }
        public int Quantity { get; set; }

        // join thêm để hiển thị
        public string ProductName { get; set; } = "";
        public string Photo { get; set; } = "";
        public decimal SalePrice { get; set; }
    }
}
