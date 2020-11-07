using System;
using System.Threading;
using System.Threading.Tasks;
using Websocket.Client;
using System.Timers;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Collections.Generic;

namespace nodejs_tests
{
    class Config
    {
        public static string id = "CSWDKW-Probook";
        public static string server = "ws://localhost:8080/";
        public static string logPath = AppDomain.CurrentDomain.BaseDirectory + "\\log";
        public static string confPath = AppDomain.CurrentDomain.BaseDirectory + "\\conf";
    }
    public class Ping
    {
        public string type = "pong";
    }
    public class ClientBulk
    {
        public string type = "client-bulk";
        public string time
        {
            get;
            set;
        }
        public string id = Config.id;
    }
    public class HandShake
    {
        public string type = "handshake";
        public string id = Config.id;
    }
    public class Message
    {
        public string type = "client-message";
        public string id = Config.id;
        public string message
        {
            get;
            set;
        }
    }
    class Program
    {
        static async Task Main(string[] args)
        {
            var url = new Uri(Config.server);
            var exitEvent = new ManualResetEvent(false);

            using (var client = new WebsocketClient(url))
            {

                client.MessageReceived.Subscribe(async msg => {
                    Console.WriteLine($"Message: {msg}");
                    dynamic message = JsonConvert.DeserializeObject($"{msg}");
                    string type_;
                    try
                    {
                        type_ = message.type;
                    }
                    catch
                    {
                        type_ = "";
                    }
                    switch (type_)
                    {
                        case "request-bulk":
                            OnBulkRequest(message,client);
                            break;
                        case "ping":
                            Ping(message, client);
                            break;
                        case "command":
                            OnCommand(message);
                            break;
                        case "shell":
                            await RunShell(message, client);
                            break;
                    }
                  
                });
                await client.Start();

                Handshake(client);

                exitEvent.WaitOne();
            }
            Console.ReadLine();
        }
        static void Handshake(WebsocketClient Client)
        {
            HandShake shake = new HandShake();
            Client.Send(JsonConvert.SerializeObject(shake));
        }
        static void OnCommand(dynamic Message)
        {
            Console.WriteLine("Got a command");
            string data = Message.data;
            List<string> argus = Library.stringParse(data);
            Console.WriteLine(argus);
            if(argus.Count == 3)
            {
                if (argus[2] == "uri")
                {
                    argus[1] = Library.PathToURI(argus[1]);
                }
            }
            Process.Start(argus[0], argus[1]);
        }
        static void Ping(dynamic Message, WebsocketClient Client)
        {
            Console.WriteLine("Got a ping");
            Ping ping = new Ping();
            Client.Send(JsonConvert.SerializeObject(ping));
        }
        static void OnBulkRequest(dynamic Message, WebsocketClient Client)
        {
            string time = Message.time;
            ClientBulk bulk = new ClientBulk { time = time };
            Client.Send(JsonConvert.SerializeObject(bulk));
        }
        static async Task RunShell(dynamic Message, WebsocketClient Client)
        {
            string script = Message.data;
            Library.Res result = Library.RunScript(script);
            string message;
            if (result.HadErr)
            {
                message = "The command was unsucessfull \n" + result.OutString;
            }
            else
            {
                message = "The command was sucessfull \n" + result.OutString;
            }
            Message msg = new Message { message = message };
            Client.Send(JsonConvert.SerializeObject(msg));
        }
    }
}
