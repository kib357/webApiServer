using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace webApiServer.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public decimal Price { get; set; }

        public Product()
        {
            var bacnet = new BacNet();
            bacnet[1700].Objects["AV21"].Create();
        }
    }
}