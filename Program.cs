using WebServer.Services;

int port = 5050; // Default port
string localAddr = "127.0.0.1"; // Default IP address

void ParseArgs(string[] args)
{
    if (args.Length >= 1)
    {
        if (int.TryParse(args[0], out int parsedPort))
        {
            port = parsedPort;
        }

        if (args.Length >= 2)
        {
            localAddr = args[1];
        }
    }
    else
    {
        Console.WriteLine("No command-line arguments provided. Using default values.");
        Console.WriteLine($"Default Port: {port}");
        Console.WriteLine($"Default IP Address: {localAddr}");
    }
}

ParseArgs(args);
Console.WriteLine("Press any key to start the web server...");
Console.ReadKey();
Server.StartServer(port, localAddr);
Console.WriteLine("Press any key to stop the web server...");
Console.ReadKey();
Server.StopServer();