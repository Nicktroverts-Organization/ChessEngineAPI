using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Reflection.Metadata.Ecma335;
using System.Security.Principal;

namespace ChessEngineAPI.App;

public static class GlobalVars
{

    private static int _Port = 5555;
    public static int Port
    {
        get
        {
            if (_Port != 0) return _Port;
            _Port = FreeTcpPort();
            return _Port;
        }
        set => _Port = value;
    }

    public static string StockFishPath = "";
    public static string PortPath = $"{Directory.GetCurrentDirectory()}/Port";
    public static Dictionary<string, PyProcess> PyProcesses = new Dictionary<string, PyProcess>();

    private static int FreeTcpPort()
    {
        TcpListener l = new TcpListener(IPAddress.Loopback, 0);
        l.Start();
        int port = ((IPEndPoint)l.LocalEndpoint).Port;
        l.Stop();
        return port;
    }
}

public class Requestable<T, T2> : Attribute
{
    public Action<T, T2> _Action;

    public Requestable()
    {

    }
}

public class App
{
    public static Dictionary<string, Action<string, string>> Methods = new();
    public Task ListeningTask;
    public static string LastEngineMove = "";

    public App()
    {
        if (!HttpListener.IsSupported)
        {
            Console.WriteLine("You don't meet the requirements to run this application! \nApplication will close in 5 seconds...");
            Thread.Sleep(new TimeSpan(0, 0, 0, 5));
            Program.ExitApplication();
            return;
        }
        InitializeMethods();
        InitializeHttpListener();
    }

    private static void InitializeMethods()
    {
        Methods.Add("HELLO", (value, value2) => HelloRequest(value, value2));
        Methods.Add("ECHO", (value, value2) => EchoRequest(value, value2));
        Methods.Add("INITIALIZE", (value, value2) => InitializeRequest(value, value2));
        Methods.Add("MAKEMOVE", (value, value2) => MakeMoveRequest(value, value2));
        Methods.Add("GETENGINEMOVE", (value, value2) => EngineMoveRequest(value, value2));
        Methods.Add("CREATEINSTANCE", (value, value2) => CreateInstanceRequest(value, value2));
        Methods.Add("REMOVEINSTANCE", (value, value2) => RemoveInstanceRequest(value, value2));
    }

    [Requestable<string, string>]
    private static void RemoveInstanceRequest(string key, string _Input)
    {
        GlobalVars.PyProcesses[key].GetProcess.Kill(true);
        GlobalVars.PyProcesses.Remove(key);
    }
    [Requestable<string, string>]
    private static void CreateInstanceRequest(string key, string _Input)
    {
        return;
    }
    [Requestable<string, string>]
    private static void InitializeRequest(string key, string _Input) => GlobalVars.Port = 0;
    [Requestable<string, string>]
    private static void EchoRequest(string key, string _Input) => Console.WriteLine($"\n---\n-{_Input}\n---");

    [Requestable<string, string>]
    private static void HelloRequest(string key, string _Input) => Console.WriteLine("\n---_---\n|Hello|\n---_---\n");

    [Requestable<string, string>]
    private static void MakeMoveRequest(string key, string _Input) => GlobalVars.PyProcesses[key].MakeMove(_Input);

    [Requestable<string, string>]
    private static void EngineMoveRequest(string key, string _Input)
    {
        return;
    }

    private void InitializeHttpListener()
    {
        var HTTPListener = new HttpListener();
        ListeningTask = new Task(() => Listener(HTTPListener));
        ListeningTask.Start();
    }

    private static void AddBasePrefixes(HttpListener listener)
    {
        listener.Prefixes.Add($"http://localhost:{GlobalVars.Port}/");
        if (new WindowsPrincipal(WindowsIdentity.GetCurrent())
            .IsInRole(WindowsBuiltInRole.Administrator))
        {
            string OwnIP = "";
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    OwnIP = ip.ToString();
                }
            }
            listener.Prefixes.Add($"http://{OwnIP}:{GlobalVars.Port}/");
        }
    }

    private static void RemoveBasePrefixes(HttpListener listener) => listener.Prefixes.Clear();

    private static Task Listener(HttpListener listener)
    {
        AddBasePrefixes(listener);
        listener.Start();
        Console.WriteLine("HttpListener is listening...\n");
        while (Program.IsApplicationRunning)
        {
            HttpListenerContext context = listener.GetContext();
            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;

            StreamReader reader = new StreamReader(request.InputStream);
            string requestContent = reader.ReadToEnd();

            if (GlobalVars.Port == 5555 && !requestContent.Contains("INITIALIZE~"))
            {
                response.Headers.Add(HttpResponseHeader.Allow, "0");
                response.Close();
                continue;
            }

            string responseMessage = "Placeholder";

            if (requestContent.Contains('~'))
            {
                string[] SplitRequest = requestContent.Split("~", StringSplitOptions.None);
                string requestContentCall = SplitRequest[0];
                string requestKey = SplitRequest[1] ?? "";
                string requestContentJson = "";
                if (SplitRequest.Length > 2)
                {
                    for (int i = 2; i < SplitRequest.Length; i++)
                    {
                        requestContentJson += SplitRequest[i];
                        if (i != SplitRequest.Length - 1)
                            requestContentJson += "~";
                    }
                }

                if (!GlobalVars.PyProcesses.ContainsKey(requestKey) && requestContentCall != "CREATEINSTANCE" &&
                    requestContentCall != "INITIALIZE")
                {
                    response.Headers.Add(HttpResponseHeader.Allow, "0");
                    response.Close();
                    continue;
                }

                if (Methods.TryGetValue(requestContentCall, out Action<string, string>? value))
                {
                    value.Invoke(requestKey, requestContentJson);
                    switch (requestContentCall)
                    {
                        case "CREATEINSTANCE":
                            responseMessage = Program.OpenPY();
                            break;
                        case "GETENGINEMOVE":
                            responseMessage = GlobalVars.PyProcesses[requestKey].GetLastEngineMove;
                            break;
                        case "INITIALIZE":
                            responseMessage = GlobalVars.Port.ToString();
                            response.Headers.Add("Access-Control-Allow-Origin", "*");
                            string InresponseString = $"{responseMessage}";
                            byte[] Inbuffer = System.Text.Encoding.UTF8.GetBytes(InresponseString);
                            response.ContentLength64 = Inbuffer.Length;
                            Stream Inoutput = response.OutputStream;
                            Inoutput.Write(Inbuffer, 0, Inbuffer.Length);
                            Inoutput.Close();
                            response.Close();
                            listener.Stop();
                            RemoveBasePrefixes(listener);
                            AddBasePrefixes(listener);
                            listener.Start();
                            continue;
                        default:
                            responseMessage = "Success.";
                            break;
                    }
                }
            }

            response.Headers.Add("Access-Control-Allow-Origin", "*");
            string responseString = $"{responseMessage}";
            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
            response.ContentLength64 = buffer.Length;
            Stream output = response.OutputStream;
            output.Write(buffer, 0, buffer.Length);
            output.Close();
            response.Close();
        }
        Console.WriteLine("HttpListener stopped listening...\n");
        listener.Stop();

        return Task.CompletedTask;
    }
}
