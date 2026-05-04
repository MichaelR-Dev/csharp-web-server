using System.Net;
using System.Net.Sockets;
using System.Text;

namespace WebServer.Services
{
    public static class Server
    {
        private static TcpListener? listener = null; // Declare the TcpListener at the class level
        private static bool isServerStarted = false; // Flag to indicate if the server is running
        private static readonly string WebServerPath = @"WebServer"; // Base path for the web server files

        /// <summary>
        /// Starts the web server with the specified parameters. If the parameters are not valid, it will print an error message and return without starting the server.
        /// </summary>
        /// <param name="port"></param>
        /// <param name="localAddr"></param>
        /// <param name="WebServerPath"></param>
        public static void StartServer(int port = 5050, string localAddr = "127.0.0.1")
        {
            if (IPAddress.TryParse(localAddr, out IPAddress? parsedAddr) && parsedAddr != null && port > 0 && port <= 65535)
            {
                if (listener != null)
                {
                    listener.Stop(); // Stop the existing listener if it's running
                    listener.Dispose(); // Dispose of the existing listener to free resources
                }

                listener = new TcpListener(IPAddress.Parse(localAddr), port);
                listener.Start();

                Console.WriteLine($"Web Server Running on {localAddr} on port {port}... Press ^C to Stop...");
                _ = StartListen(); // Start listening for incoming connections asynchronously
            }
            else
            {
                Console.WriteLine("Invalid IP address format or port out of range.");
                return;
            }
        }

        public static void StopServer()
        {
            if (listener != null)
            {
                isServerStarted = false; // Set the running flag to false to stop the server loop
                listener.Stop(); // Stop the TcpListener
                listener.Dispose(); // Dispose of the TcpListener to free resources
                Console.WriteLine("Web Server stopped.");
            }
            else
            {
                Console.WriteLine("Server is not running.");
            }
        }

        /// <summary>
        /// Listens for incoming TCP connections and processes HTTP requests. It reads the request, parses the headers, and sends appropriate responses based on the request type and headers. The method runs indefinitely until the server is stopped.
        /// </summary>
        private static async Task StartListen()
        {
            isServerStarted = true;

            while (isServerStarted && listener != null)
            {
                try
                {
                    TcpClient client = await listener.AcceptTcpClientAsync();
                    _ = Task.Run(() => HandleClient(client)); // Handle each client connection in a separate task
                }
                catch
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Determines the content type based on the file extension of the requested file. It uses a switch expression to map common file extensions to their corresponding MIME types. If the file extension is not recognized, it defaults to "application/octet-stream".
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        private static string GetContentType(string filePath)
        {
            if (filePath == "/")
                filePath = "/index.html";

            string extension = Path.GetExtension(filePath).ToLower();
            return extension switch
            {
                ".html" => "text/html",
                ".css" => "text/css",
                ".js" => "application/javascript",
                ".png" => "image/png",
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".json" => "application/json",
                _ => "application/octet-stream",
            };
        }

        /// <summary>
        /// Handles an individual client connection by reading the HTTP request, parsing the headers, and sending appropriate responses based on the request type and headers. It supports handling GET requests and sends a 404 response for unsupported methods or if the requested content is not found. After processing the request, it closes the client connection.
        /// </summary>
        /// <param name="client"></param>
        private static void HandleClient(TcpClient client)
        {
            NetworkStream stream = client.GetStream();

            //read request 
            string request = ReadRequest(stream);
            (Dictionary<string, string> requestHeaders, string requestType, string requestMethod) = ParseHeaders(request);

            string contentType = GetContentType(requestType.Split(' ')[1]);
            string contentEncoding = requestHeaders.GetValueOrDefault("Accept-Encoding")!;

            switch (requestMethod)
            {
                case "GET":
                    HandleGetRequest(requestType, contentType, contentEncoding, ref stream);
                    break;
                default:
                    SendHeaders("HTTP/1.1", 405, "Method Not Allowed", contentType, [], stream);
                    break;
            }

            client.Close();
        }

        private static string ReadRequest(NetworkStream stream)
        {
            byte[] buffer = new byte[4096];
            int bytesRead = stream.Read(buffer, 0, buffer.Length);
            return Encoding.UTF8.GetString(buffer, 0, bytesRead);
        }

        /// <summary>
        /// Sends HTTP response headers and body to the client through the provided network stream. It constructs the HTTP response header based on the status code, status message, content type, content encoding, and content length. The method then writes the header and body bytes to the network stream and flushes it to ensure that the response is sent to the client.
        /// </summary>
        /// <param name="statusCode"></param>
        /// <param name="statusMsg"></param>
        /// <param name="contentType"></param>
        /// <param name="body"></param>
        /// <param name="networkStream"></param>
        private static void SendHeaders(
            string httpVersion,
            int statusCode,
            string statusMsg,
            string contentType,
            byte[] body,
            NetworkStream networkStream)
        {
            string header =
                $"{httpVersion} {statusCode} {statusMsg}\r\n" +
                $"Date: {DateTime.UtcNow:R}\r\n" +
                $"Server: CustomDotNetServer\r\n" +
                $"Content-Type: {contentType}\r\n" +
                $"Content-Length: {body.Length}\r\n" +
                $"Connection: close\r\n\r\n";

            byte[] headerBytes = Encoding.UTF8.GetBytes(header);

            networkStream.Write(headerBytes, 0, headerBytes.Length);
            networkStream.Write(body, 0, body.Length);
            networkStream.Flush();
        }

        /// <summary>
        /// Parses the HTTP request headers from the provided header string. It splits the header string into individual lines, extracts the first line to determine the request type, and then iterates through the remaining lines to populate a dictionary with header names and their corresponding values. The method returns a tuple containing the dictionary of headers and the request type extracted from the first line of the header string.
        /// </summary>
        /// <param name="headerString"></param>
        /// <returns></returns>
        private static (Dictionary<string, string> headers, string requestType, string requestMethod) ParseHeaders(string headerString)
        {
            var headerLines = headerString.Split('\r', '\n');
            string requestType = headerLines[0];
            string requestMethod = requestType.Split(' ')[0];
            var headerValues = new Dictionary<string, string>();

            foreach (var headerLine in headerLines)
            {
                var headerDetail = headerLine.Trim();
                var delimiterIndex = headerLine.IndexOf(':');
                if (delimiterIndex >= 0)
                {
                    var headerName = headerLine[..delimiterIndex].Trim();
                    var headerValue = headerLine[(delimiterIndex + 1)..].Trim();
                    headerValues.Add(headerName, headerValue);
                }
            }

            return (headerValues, requestType, requestMethod);
        }

        /// <summary>
        /// Retrieves the content of a requested file based on the provided path. If the requested path is "/", it defaults to "index.html". The method constructs the full file path by joining the web server's base path with the requested path. It checks if the file exists at the constructed path, and if it does, it reads the file's bytes and returns them. If the file does not exist, it returns null.
        /// </summary>
        /// <param name="requestedPath"></param>
        /// <returns></returns>
        private static byte[]? GetContent(string requestedPath)
        {
            if (requestedPath == "/") requestedPath = "index.html";
            string filePath = Path.Join(WebServerPath, requestedPath);

            if (!File.Exists(filePath)) return null;

            else
            {
                byte[] file = System.IO.File.ReadAllBytes(filePath);
                return file;
            }
        }

        /// <summary>
        /// Handles HTTP GET requests by retrieving the requested content and sending appropriate responses back to the client. It extracts the requested path from the request type, retrieves the content using the GetContent method, and checks if the content is not null. If the content exists, it sends a 200 OK response with the content type and encoding headers, followed by writing the content bytes to the network stream. If the content does not exist, it sends a 404 Page Not Found response with the appropriate headers.
        /// </summary>
        /// <param name="requestType"></param>
        /// <param name="contentType"></param>
        /// <param name="contentEncoding"></param>
        /// <param name="networkStream"></param>
        private static void HandleGetRequest(string requestType, string contentType, string contentEncoding, ref NetworkStream networkStream)
        {
            var requestedPath = requestType.Split(' ')[1];
            var fileContent = GetContent(requestedPath);
            if(fileContent is not null)
            {
                SendHeaders("HTTP/1.1", 200, "OK", contentType, fileContent, networkStream);
                networkStream.Write(fileContent, 0, fileContent.Length);
            }
            else
            {
                byte[] body = Encoding.UTF8.GetBytes("<html><body><h1>404 Page Not Found</h1></body></html>");
                SendHeaders("HTTP/1.1", 404, "Not Found", contentType, body, networkStream);
            }
        }
    }
}