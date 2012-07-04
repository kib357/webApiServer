using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.WebSockets;

namespace webApiServer
{
    /// <summary>
    /// Summary description for Subscribe
    /// </summary>
    public class Subscribe : IHttpHandler
    {
        // Список всех клиентов
        private static readonly List<WebSocket> Clients = new List<WebSocket>();

        // Блокировка для обеспечения потокабезопасности
        private static readonly ReaderWriterLockSlim Locker = new ReaderWriterLockSlim();

        #region IHttpHandler Members

        public void ProcessRequest(HttpContext context)
        {
            if (context.IsWebSocketRequest)
                context.AcceptWebSocketRequest(WebSocketRequest);
            //context.Response.ContentType = "text/plain";
            //context.Response.Write("Hello World");
        }

        public bool IsReusable
        {
            get { return false; }
        }

        #endregion

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