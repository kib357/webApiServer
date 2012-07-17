using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BacNetApi;

namespace webApiServer.Models
{
    public static class BacNetModel
    {
        public static BacNet Network = new BacNet("192.168.0.101");
    }
}