using System;
using BacNetApi;
using BacNetApi.Data;

namespace LigtService.Common
{
	public static class Helper
	{
		public static PrimitiveObject GetObject(this BacNet net, string address)
		{
			var addressList = address.Split('.');
			uint dev;
			if (addressList.Length != 2 || !uint.TryParse(addressList[0], out dev))
				throw new Exception("проверь как забил адреса");
			return net[dev].Objects[addressList[1]];
		}
	}
}
