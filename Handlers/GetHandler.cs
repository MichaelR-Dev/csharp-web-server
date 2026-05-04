namespace WebServer.Handlers;
public class GetHandler implements IHandler
{
    /// <summary>
    /// Handles HTTP GET requests by retrieving the requested content and sending appropriate responses back to the client. It extracts the requested path from the request type, retrieves the content using the GetContent method, and checks if the content is not null. If the content exists, it sends a 200 OK response with the content type and encoding headers, followed by writing the content bytes to the network stream. If the content does not exist, it sends a 404 Page Not Found response with the appropriate headers.
    /// </summary>
    /// <param name="requestType"></param>
    /// <param name="contentType"></param>
    /// <param name="contentEncoding"></param>
    /// <param name="networkStream"></param>
    private void HandleRequest(string requestType, string contentType, string contentEncoding, ref NetworkStream networkStream)
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