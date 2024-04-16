using System;
using System.Diagnostics;
using System.Net.Http.Json;
using System.Runtime.InteropServices;
using ChessEngineAPI.App;

AppDomain.CurrentDomain.ProcessExit += CloseHandler;

Console.WriteLine("Initializing application...\n");

mainApp = new App();

Console.WriteLine($"\nFinished initialization of application at port :{GlobalVars.Port}.");

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

    public static string OpenPY()
    {
        //Open python script that runs chess engine.
        ProcessStartInfo start = new ProcessStartInfo();
        start.FileName = "C:\\Users\\Praktikant\\AppData\\Local\\Microsoft\\WindowsApps\\PythonSoftwareFoundation.Python.3.11_qbz5n2kfra8p0\\python.exe";
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

