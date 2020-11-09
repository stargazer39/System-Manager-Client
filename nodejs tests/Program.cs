using System;
using System.Threading;
using System.Threading.Tasks;
using Websocket.Client;
using System.Timers;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;

namespace nodejs_tests
{
    class Config
    {
        public static string id = "CSWDKW-Probook";
        public static string server = "ws://localhost:8080/";
        public static string logPath = AppDomain.CurrentDomain.BaseDirectory + "\\log";
        public static string confPath = AppDomain.CurrentDomain.BaseDirectory + "\\conf";
        public static bool connected = false;
        public static string time;
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
        public string user = "client";
        public string id = Config.id;
        public string time = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
    }
    public class Message
    {
        public string type = "message";
        public string id = Config.id;
        public string user = "client";
        public string data
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
                client.MessageReceived.Subscribe(async msg =>
                {
                    Console.WriteLine($"Message: {msg}");
                    dynamic message = JsonConvert.DeserializeObject($"{msg}");
                    string type;
                    try
                    {
                        type = message.type;
                    }
                    catch
                    {
                        type = "";
                    }
                    switch (type)
                    {
                        case "request":
                            Handshake(client);
                            break;
                        case "handshake":
                            ShakeVerify(message);
                            break;
                        case "ping":
                            Ping(message, client);
                            break;
                        case "command":
                            OnCommand(message, client);
                            break;
                        case "shell":
                            await RunShell(message, client);
                            break;
                        case "files":
                            await FileOperations(message, client);
                            break;
                    }

                });
                //client.ErrorReconnectTimeout = TimeSpan.FromSeconds(5);
                await client.Start();

                exitEvent.WaitOne();
            }
            Console.ReadLine();
        }
        static void ShakeVerify(dynamic Message)
        {
            try
            {
                string time = Message.time;
                string id = Message.data;
                if (Config.time == time && Config.id == id)
                {
                    Config.connected = true;
                    Console.WriteLine("Connected to the server");
                }
                else
                {
                    throw new ArgumentException("Time or ID dosen't Match Up. Nice try.", "original");
                }
            }
            catch(Exception e)
            {
                Console.WriteLine( "Connection Verification Faild Due to :" +  e.Message);
            }
        }
        static void Handshake(WebsocketClient Client)
        {
            HandShake shake = new HandShake();
            Config.time = shake.time;
            Client.Send(JsonConvert.SerializeObject(shake));
        }
        static void OnCommand(dynamic Message, WebsocketClient Client)
        {
                Console.WriteLine("Got a command");
                string message = null;
                try
                {
                    Newtonsoft.Json.Linq.JArray data = Message.data;
                    List<string> aargs = Library.JtokenToList(data);
                    if (aargs.Count == 3)
                    {
                        if (aargs[2].ToString() == "uri")
                        {
                            aargs[1] = Library.PathToURI(aargs[1]);
                        }
                    }
                    Process.Start(aargs[0], aargs[1]);
                    //Message msg = new Message { data = message, from = "shell" };
                    //Client.Send(JsonConvert.SerializeObject(msg));
                    message = "The Command was successful";
                }
                catch (Exception err)
                {
                    message = $"The command was unuccessful. Error :: \n {err.Message}";
                }
                Console.WriteLine(message);
                GenericCommand com = new GenericCommand { type = "command", data = message, haderror = true };
                Message msg = new Message { data = JsonConvert.SerializeObject(com) };
                Client.Send(JsonConvert.SerializeObject(msg));
        }
        static void Ping(dynamic Message, WebsocketClient Client)
        {
            Message msg = new Message { data = "pong" };
            Client.Send(JsonConvert.SerializeObject(msg));
        }
        class GenericCommand
        {
            public string type
            {
                get;
                set;
            }
            public bool haderror
            {
                get;
                set;
            }
            public string data
            {
                get;
                set;
            }
        }
        static async Task RunShell(dynamic Message, WebsocketClient Client)
        {
            Console.WriteLine("Trying to run Shell");
            string script = Message.data;
            Library.Res result = Library.RunScript(script);
            string message;
            if (result.HadErr)
            {
                message = "The Command was unuccessful \n" + result.OutString;
            }
            else
            {
                message = "The Command was successful \n" + result.OutString;
            }
            GenericCommand com = new GenericCommand { type = "shell", data = message , haderror = result.HadErr };
            Message msg = new Message { data = JsonConvert.SerializeObject(com) };
            Client.Send(JsonConvert.SerializeObject(msg));
        }
        static async Task FileOperations(dynamic Message, WebsocketClient Client)
        {
            string path = Message.data;
            try
            {
                int index = Message.index;
                DirectoryInfo info = new DirectoryInfo(path);
                DirectoryInfo[] dinfo = info.GetDirectories();
                List<string> strArr = new List<string>();
                foreach (DirectoryInfo dirr in dinfo)
                {
                    Console.WriteLine(dirr.Name);
                    strArr.Add(dirr.Name);
                }
                path = path + "\\" + strArr[index];
            }
            catch
            {

            }
            DirectoryInfo info2 = new DirectoryInfo(path);
            DirectoryInfo[] dinfo2 = info2.GetDirectories();
            List<string> strArr2 = new List<string>();
            foreach (DirectoryInfo dirr in dinfo2)
            {
                Console.WriteLine(dirr.Name);
                strArr2.Add(dirr.Name);
            }
            //Message msg = new Message { message = JsonConvert.SerializeObject(strArr2), from = "files" };
            //Client.Send(JsonConvert.SerializeObject(msg));
        }
    }
}
