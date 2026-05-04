interface IHandler
{
    void HandleRequest(string requestType, string contentType, string contentEncoding, ref NetworkStream networkStream);
}