using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

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

    private static int FreeTcpPort()
    {
        TcpListener l = new TcpListener(IPAddress.Loopback, 0);
        l.Start();
        int port = ((IPEndPoint)l.LocalEndpoint).Port;
        l.Stop();
        return port;
    }
}

public class Requestable<T> : Attribute
{
    public Action<T> _Action;

    public Requestable()
    {

    }
}

public class App
{
    public static Dictionary<string, Action<string>> Methods = new();
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
        Methods.Add("HELLO", value => HelloRequest(value));
        Methods.Add("ECHO", value => EchoRequest(value));
        Methods.Add("INITIALIZE", value => InitializeRequest(value));
        Methods.Add("MAKEMOVE", value => MakeMoveRequest(value));
        Methods.Add("GETENGINEMOVE", value => EngineMoveRequest(value));
    }

    [Requestable<string>]
    private static void InitializeRequest(string _Input) => GlobalVars.Port = 0;
    [Requestable<string>]
    private static void EchoRequest(string _Input) => Console.WriteLine($"\n---\n-{_Input}\n---");

    [Requestable<string>]
    private static void HelloRequest(string _Input) => Console.WriteLine("\n---_---\n|Hello|\n---_---\n");

    [Requestable<string>]
    private static void MakeMoveRequest(string _Input)
    {
        Program.PyProcess.StandardInput.WriteLine(_Input);

        StreamReader reader = new StreamReader(Program.PyProcess.StandardOutput.BaseStream);
        LastEngineMove = reader.ReadLine().ToString();
    }

    [Requestable<string>]
    private static void EngineMoveRequest(string _Input)
    {
        if (LastEngineMove == "")
            throw new Exception("Called to early. Engine didn't move yet.");
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
        listener.Prefixes.Add($"http://localhost:{GlobalVars.Port}/");
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
                string requestContentCall = requestContent.Split("~", StringSplitOptions.None)[0];
                string requestContentJson = requestContent.Split("~", StringSplitOptions.None)[1];

                if (Methods.TryGetValue(requestContentCall, out Action<string>? value))
                {
                    value.Invoke(requestContentJson);
                    switch (requestContentCall)
                    {
                        case "GETENGINEMOVE":
                            responseMessage = LastEngineMove;
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
