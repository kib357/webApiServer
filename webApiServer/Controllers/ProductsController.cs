using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using webApiServer.Models;

namespace webApiServer.Controllers
{
    public class ProductsController : ApiController
    {
        private readonly Product[] _products = new Product[]
            {
                new Product {Id = 1, Name = "Tomato Soup", Category = "Groceries", Price = 1.39M},
                new Product {Id = 2, Name = "Yo-yo", Category = "Toys", Price = 3.75M},
                new Product {Id = 3, Name = "Hammer", Category = "Hardware", Price = 16.99M}
            };

        public IEnumerable<Product>  GetAllProducts()
        {
            return _products;
        }

        public Product GetProductByid(int id)
        {
            var product = _products.FirstOrDefault(p => p.Id == id);
            if (product == null)
            {
                var resp = new HttpResponseMessage(HttpStatusCode.NotFound);
                throw new HttpResponseException(resp);
            }
            return product;
        }

        public IEnumerable<Product> GetProductsByCategory(string category)
        {
            return _products.Where(
                (p) => string.Equals(p.Category, category,
                    StringComparison.OrdinalIgnoreCase));
        }

    }
}
