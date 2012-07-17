using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.WebSockets;
using BacNetApi;
using webApiServer.Models;

namespace webApiServer.Controllers
{
    public class BacNetController : ApiController
    {
        // Список всех клиентов
        private static readonly List<WebSocket> Clients = new List<WebSocket>();

        //private static BacNet _network = new BacNet("192.168.0.101");

        // Блокировка для обеспечения потокабезопасности
        private static readonly ReaderWriterLockSlim Locker = new ReaderWriterLockSlim();

        public string Get()
        {
            return "BACnet";
        }

        public string GetById(string id)
        {            
            //return _network[200].Objects["AV1"].Get().ToString();
            object k = BacNetModel.Network[200].Objects["AV1"].GetAsync().Result;
            if (k != null) return k.ToString();
            return "Error";
        }

        public void Get(string subscribe)
        {
            var context = HttpContext.Current;
            if (context.IsWebSocketRequest)
                context.AcceptWebSocketRequest(WebSocketRequest);
            var resp = new HttpResponseMessage(HttpStatusCode.SwitchingProtocols);
            throw new HttpResponseException(resp);
        }

        private async Task WebSocketRequest(AspNetWebSocketContext context)
        {
            // Получаем сокет клиента из контекста запроса
            WebSocket socket = context.WebSocket;

            // Добавляем его в список клиентов
            Locker.EnterWriteLock();
            try
            {
                Clients.Add(socket);
            }
            finally
            {
                Locker.ExitWriteLock();
            }

            // Слушаем его
            while (true)
            {
                var buffer = new ArraySegment<byte>(new byte[1024]);
                // Ожидаем данные от него
                WebSocketReceiveResult result = await socket.ReceiveAsync(buffer, CancellationToken.None);

                //Передаём сообщение всем клиентам
                for (int i = 0; i < Clients.Count; i++)
                {
                    WebSocket client = Clients[i];
                    try
                    {
                        if (client.State == WebSocketState.Open)
                        {
                            await client.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
                        }
                    }
                    catch (ObjectDisposedException)
                    {
                        Locker.EnterWriteLock();
                        try
                        {
                            Clients.Remove(client);
                            i--;
                        }
                        finally
                        {
                            Locker.ExitWriteLock();
                        }
                    }
                }
            }
        }
    }
}
