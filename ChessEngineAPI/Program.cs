using System.ComponentModel;
using System.Diagnostics;
using ChessEngineAPI.App;

AppDomain.CurrentDomain.ProcessExit += CloseHandler;

Console.WriteLine("Initializing application...\n");

string PythonPath = "";

try
{
    FileStream fs = File.OpenRead("PythonPath");
    StreamReader sreader = new StreamReader(fs);
    PythonPath = Convert.ToString(sreader.ReadToEnd());
    sreader.Close();
    fs.Close();
}
catch
{
    Console.WriteLine("\nPython Path is invalid. Please enter new Path: ");
    PythonPath = Console.ReadLine();
    FileStream _fs = File.Create("PythonPath");
    StreamWriter swrite = new StreamWriter(_fs);
    swrite.WriteLine(PythonPath);
    swrite.Close();
    _fs.Close();
}

Process process;

try
{
    process = Process.Start(PythonPath);
    process.Kill(true);
}
catch
{
    Console.WriteLine("\nPython Path is invalid. Please enter new Path: ");
    PythonPath = Console.ReadLine();
    FileStream _fs = File.Create("PythonPath");
    StreamWriter swrite = new StreamWriter(_fs);
    swrite.WriteLine(PythonPath);
    swrite.Close();
    _fs.Close();
}

string StockFishPath = "";

try
{
    FileStream fs2 = File.OpenRead("StockFishPath");
    StreamReader sreader2 = new StreamReader(fs2);
    StockFishPath = Convert.ToString(sreader2.ReadToEnd());
    sreader2.Close();
    fs2.Close();

}
catch
{
    Console.WriteLine("\nStockFish Path is invalid. Please enter new Path: ");
    StockFishPath = Console.ReadLine();
    FileStream _fs = File.Create("StockFishPath");
    StreamWriter swrite = new StreamWriter(_fs);
    swrite.WriteLine(StockFishPath);
    swrite.Close();
    _fs.Close();
}

Process process2;

try
{
    process2 = Process.Start(PythonPath);
    process2.Kill(true);
}
catch
{
    Console.WriteLine("\nStockFish Path is invalid. Please enter new Path: ");
    StockFishPath = Console.ReadLine();
    FileStream _fs = File.Create("StockFishPath");
    StreamWriter swrite = new StreamWriter(_fs);
    swrite.WriteLine(StockFishPath);
    swrite.Close();
    _fs.Close();
}


mainApp = new App();

Console.WriteLine($"\n\nFinished initialization of application at port :{GlobalVars.Port}.");

while (ApplicationIsRunning)
{
    string UInput = Console.ReadLine();
    if (UInput.ToLower() == "exit")
    {
        ExitApplication();
        continue;
    }

    using StringContent content = new StringContent(UInput);

    HttpClient client = new HttpClient();
    using HttpResponseMessage response = client.PostAsync($"http://localhost:{GlobalVars.Port}/", content).Result;
    response.EnsureSuccessStatusCode();
    string responseBody = response.Content.ReadAsStringAsync().Result;
    Console.WriteLine(responseBody);
}

public partial class Program
{
    public static App mainApp;
    private static bool ApplicationIsRunning = true;

    public static bool IsApplicationRunning
    {
        get => ApplicationIsRunning;
        set => throw new UnauthorizedAccessException("Can't set this!");
    }

    private static void CloseHandler(object? obj, EventArgs args)
    {
        ExitApplication();
    }

    public static void ExitApplication()
    {
        Console.WriteLine("\nStopping application...\n");
        foreach (var VARIABLE in GlobalVars.PyProcesses)
        {
            VARIABLE.Value.GetProcess.Kill(true);
        }
        ApplicationIsRunning = false;
        HttpClient client = new();
        client.GetAsync($"http://localhost:{GlobalVars.Port}/");
        mainApp.ListeningTask.Wait();
        Console.WriteLine("\nApplication stopped\n");
    }

    private static void PyProcessOutputHandler(object sender, DataReceivedEventArgs args)
    {
        Console.WriteLine(sender.ToString() + " - " + args.Data.ToString());
    }

    public static bool ExistsOnPath(string exeName)
    {
        try
        {
            using (Process p = new Process())
            {
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.FileName = "where";
                p.StartInfo.Arguments = exeName;
                p.Start();
                p.WaitForExit();
                return p.ExitCode == 0;
            }
        }
        catch (Win32Exception)
        {
            throw new Exception("'where' command is not on path");
        }
    }

    public static string OpenPY()
    {
        //Open python script that runs chess engine.
        ProcessStartInfo start = new ProcessStartInfo();
        FileStream fs = File.OpenRead("PythonPath");
        StreamReader sreader = new StreamReader(fs);
        string PythonPath = Convert.ToString(sreader.ReadToEnd());
        start.FileName = PythonPath;
        start.Arguments = string.Format("{0}", "Python/ChessConnector.py");
        start.UseShellExecute = false;
        start.RedirectStandardOutput = true;
        start.RedirectStandardInput = true;
        Process process = Process.Start(start);
        string uniqueKey = Guid.NewGuid().ToString();
        PyProcess pyProcess = new PyProcess(process, uniqueKey);
        GlobalVars.PyProcesses.Add(uniqueKey, pyProcess);

        process.OutputDataReceived += PyProcessOutputHandler;

        return uniqueKey;
    }
}

