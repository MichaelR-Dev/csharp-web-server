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
        Console.WriteLine("\nNo command-line arguments provided. Using default values.");
        Console.WriteLine($"Default Port: {port}");
        Console.WriteLine($"Default IP Address: {localAddr}");
    }
}

ParseArgs(args);
Console.WriteLine("\nPress any key to start the web server...\n");
Console.ReadKey(true);
Server.StartServer(port, localAddr);

while(true)
{
    if (Console.ReadKey(true).Key == ConsoleKey.Q)
    {        
        Console.WriteLine("Shutting down the server...");
        Server.StopServer();
        break;
    }
}