﻿using System.Net.Sockets;
using System.Net;
using System.Text;
using Microsoft.Extensions.Logging;


namespace MediaServer
{
    class Server
    {
        private const int MAXCONNECTIONS = 10;
        private int connections;
        private Socket socServer;
        private string ip;
        private int port;
        private bool running;
        private FileStream servedFile = null;
        ILogger logger;
        private AvailableMedia availableMedia;
        private Semaphore maxNumberAcceptedClients;
        private IPEndPoint IPE;

        private Server()
        {
            socServer = null;
            connections = 0;
            maxNumberAcceptedClients = new Semaphore(MAXCONNECTIONS, MAXCONNECTIONS);
        }
        public Server(string ip, int port, string mediaDir) : this()
        {
            availableMedia = new AvailableMedia(mediaDir);

            using ILoggerFactory factory = LoggerFactory.Create(builder => builder
            .AddFilter("MediaServer.Server", LogLevel.Debug)
            .AddConsole());

            logger = factory.CreateLogger<Server>();
            socServer = null;
            this.ip = ip;
            this.port = port;
            socServer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPE = new IPEndPoint(IPAddress.Parse(this.ip), this.port);            
        }

        public void Start()
        {
            socServer.Bind(IPE);
            socServer.Listen(0);
            running = true;
            Thread thread = new Thread(Listen);
            thread.Start();
        }

        public void Stop()
        {
            running = false;
            Thread.Sleep(100);
            if (this.servedFile != null)
            { try { servedFile.Close(); } catch {; } }
            if (socServer != null && socServer.Connected) socServer.Shutdown(SocketShutdown.Both);
        }

		//TODO: Finish Implementation
        /// <summary>
        /// This method listens for requests
        /// Use the pattern found at https://learn.microsoft.com/en-us/dotnet/api/system.net.sockets.socketasynceventargs?view=net-9.0
        /// The Start method at the link is our Listen method   
        /// </summary>
        private void Listen()
        {
            SocketAsyncEventArgs e = new SocketAsyncEventArgs();
            e.Completed += AcceptCallback;

            bool pending = false;
            while (this.running && !pending)
            {
                Console.WriteLine("Waiting connection ...");
                maxNumberAcceptedClients.WaitOne();
                
e.AcceptSocket = null;  // Reset socket before reuse
pending = socServer.AcceptAsync(e);

                
            }
        }       

        /// <summary>
        /// Accepts the request and starts to process it on a new thread
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AcceptCallback(object sender, SocketAsyncEventArgs e)
        {
            Thread thread = new Thread(ProcessAccept);
            thread.Start(e);
        }

        private void ProcessAccept(object eObj)
        {
            SocketAsyncEventArgs e = (SocketAsyncEventArgs)eObj;
            try
            {
                Interlocked.Increment(ref connections);
                Console.WriteLine("Client connection accepted. There are {0} clients connected to the server", connections);
                Socket sock = e.AcceptSocket;
                byte[] buffer = new byte[3000];
                int receivedSize = 0;
                var sb = new StringBuilder();
                MemoryStream ms;

                while (sock.Available > 0)
                {
                    receivedSize = sock.Receive(buffer, SocketFlags.None);

                    ms = new MemoryStream();
                    ms.Write(buffer, 0, receivedSize);
                    string toAdd = UTF8Encoding.UTF8.GetString(ms.ToArray());
                    sb.Append(toAdd);
                    logger.LogDebug("Received {0} bytes", receivedSize);
                }
                string requestData = sb.ToString();

                BusinessLogic(requestData, sock);
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, "Error processing request");
            }
            finally
            {
                Listen();
            }
             
        }

        private void CloseClientSocket(Socket socket)
        {
            // close the socket associated with the client
            try
            {
                socket.Shutdown(SocketShutdown.Send);
            }
            // throws if client process has already closed
            catch (Exception) { }
            socket.Close();

            // decrement the counter keeping track of the total number of clients connected to the server
            Interlocked.Decrement(ref connections);

            maxNumberAcceptedClients.Release();
            Console.WriteLine("A client has been disconnected from the server. There are {0} clients connected to the server", connections);
        }
private void BusinessLogic(string request, Socket conn)
{
    // 1. Break the raw request into lines & headers
    var requestLines = GetRequestLines(request);
    var headers      = GetHeaders(requestLines);

    // 2. Parse the request-line ("GET /foo HTTP/1.1") into (method, path)
    var (method, path) = GetMethodAndPath(requestLines[0]);

    // 3. Dispatch on the HTTP method
    switch (method.ToUpperInvariant())
    {
        case "HEAD":
            HandleHead(conn, headers, path);
            break;

        case "GET":
            HandleGet(conn, headers, path);
            break;

    

}


 }   // closes the switch
 // closes the surrounding method (BusinessLogic/ProcessRequest)


    /// <summary>Split the raw request into lines.</summary>
    





 /// <summary>Split the raw request into lines.</summary>
private string[] GetRequestLines(string rawRequest)
    => rawRequest.Split(new[] { "\r\n" }, StringSplitOptions.None);

/// <summary>Parse headers from lines[1..] until the first blank line.</summary>
private List<KeyValuePair<string,string>> GetHeaders(string[] requestLines)
{
    var headers = new List<KeyValuePair<string,string>>();
    for (int i = 1; i < requestLines.Length && requestLines[i] != ""; i++)
    {
        var parts = requestLines[i].Split(new[] { ':' }, 2);
        if (parts.Length == 2)
            headers.Add(new(parts[0].Trim(), parts[1].Trim()));
    }
    return headers;
}

/// <summary>Parse the request-line into (Method, Path).</summary>
private (string Method, string Path) GetMethodAndPath(string requestLine)
{
    // e.g. "GET /song.mp3 HTTP/1.1"
    var parts = requestLine.Split(' ', 3);
    return (parts[0], parts[1]);
}



        //TODO: Implement
        /// <summary>
        /// Thus methods returns the HTTP method used in the request and the requested path in key value pair
        /// </summary>
        /// <param name="requestLines">The request lines</param>
        /// <returns>Key Value Pair containing the method as a key and the path as the value</returns>
      private KeyValuePair<string, string> GetMethodAndPath(string[] requestLines)
{
    if (requestLines.Length == 0) return new KeyValuePair<string, string>("", "");
    string[] tokens = requestLines[0].Split(' ');
    if (tokens.Length < 2) return new KeyValuePair<string, string>("", "");
    return new KeyValuePair<string, string>(tokens[0], tokens[1]);
}





        private int GetIndexFromPath(string path)
        {
            int index = -1;
            string requestIndex = path.Remove(0,1);
            logger.LogDebug("Attempting to find match for: {0}", requestIndex);
            if (!int.TryParse(requestIndex, out index))
            {
                logger.LogDebug("Index parsing from path failed.");
                index = -1;
            }
            return index;
        }

        //TODO: Finish implementation
        /// <summary>
        /// This method returns the header information of a particular file
        /// </summary>
        /// <param name="handler"></param>
        /// <param name="headers"></param>
        /// <param name="path"></param>
        private void HandleHead(Socket handler, List<KeyValuePair<string, string>> headers, string path)
        {
            int index = GetIndexFromPath(path);
            String requestFile = availableMedia.getAbsolutePath(index);

            logger.LogDebug("GET requested for file: {0}", requestFile);

            FileInfo fileInfo = new FileInfo(requestFile);
        if (fileInfo.Exists)
{
    string contentType = GetContentType(requestFile);
    string response = "HTTP/1.1 200 OK\r\n" +
                      $"Content-Type: {contentType}\r\n" +
                      $"Content-Length: {fileInfo.Length}\r\n" +
                      "Connection: close\r\n\r\n";
    handler.Send(Encoding.UTF8.GetBytes(response));
    CloseClientSocket(handler);
}
else
{
    CloseClientSocket(handler);
}

        }
        private void HandleGet(Socket handler, List<KeyValuePair<string, string>> headers, string path)
        {
            int index = GetIndexFromPath(path);
            if (index == -1)
            {
                ReturnList(handler); //List requested
            }
            else //File possibly requested
            {
                string requestFile = availableMedia.getAbsolutePath(index);
                logger.LogDebug("GET requested for file: {0}", requestFile);
                ServeFile(handler, headers, requestFile);
            }
        }

        //TODO: Finish implementation
        /// <summary>
        /// This method returns a  webpage contain with a list of media found in the media dir. Each entry on the page must
        /// be clickable via an anchor tag which shows the name of the media file with its href attribute set to the index
        /// number of the file. Clicking the link must open the media item.
        /// The names of the file must not show the root path to the media dir
        /// 
        /// </summary>
        /// <param name="handler">The socket to write the webpage to</param>
       private void ReturnList(Socket handler)
{
    string template = File.ReadAllText("template.txt");
    string media = "";
    string[] files = availableMedia.getAvailableFiles().ToArray();

    for (int i = 0; i < files.Length; i++)
    {
        string fileName = Path.GetFileName(files[i]);
        media += $"<a href=\"/{i}\">{fileName}</a><br/>";
    }

    template = template.Replace("[MEDIA_LIST]", media);

    string ContentType = "text/html";
    string Reply = "HTTP/1.1 200 OK" + Environment.NewLine + "Server: VLC" + Environment.NewLine + "Content-Type: " + ContentType + Environment.NewLine;
    Reply += "Last-Modified: " + GMTTime(DateTime.Now) + Environment.NewLine;
    Reply += "Date: " + GMTTime(DateTime.Now) + Environment.NewLine;
    Reply += "Accept-Ranges: bytes" + Environment.NewLine;
    UTF8Encoding encoding = new UTF8Encoding();
    byte[] bytes = encoding.GetBytes(template);
    long length = bytes.Length;
    Reply += "Content-Length: " + length + Environment.NewLine;
    Reply += "Connection: close" + Environment.NewLine + Environment.NewLine;
    handler.Send(UTF8Encoding.UTF8.GetBytes(Reply), SocketFlags.None);
    handler.Send(bytes);
    CloseClientSocket(handler);
}


		//TODO: Finish implementation
        /// <summary>
        /// This method determines if to stream a file or send it in its entirety.
        /// </summary>
        /// <param name="handler">The socket to send the file to</param>
        /// <param name="headers">The request headers</param>
        /// <param name="requestFile">The requested file</param>
        private void ServeFile(Socket handler, List<KeyValuePair<string, string>> headers, string requestFile)
        {
            long tempRange;
            bool hasRange = false;
            KeyValuePair<string, string> rangeHeader = headers.Find(e => e.Key.Contains("range:"));
            String acceptRange;
            if (!rangeHeader.Equals(default(KeyValuePair<string, string>)))
            {
                hasRange = true;
                acceptRange = rangeHeader.Value;
            }
            
            if (hasRange)
            {
                string range = rangeHeader.Value.ToLower().ChopOffBefore("range: ").ChopOffAfter("-").ChopOffAfter(Environment.NewLine).Replace("bytes=", "");
                long.TryParse(range, out tempRange);
            }
            else
                tempRange = 0;
            FileSenderHeler fsHelper = new FileSenderHeler(requestFile, handler, tempRange);
            if (!hasRange || requestFile.ToLower().EndsWith(".jpg") || requestFile.ToLower().EndsWith(".png") || requestFile.ToLower().EndsWith(".gif") || requestFile.ToLower().EndsWith(".mp3"))
            {
                Thread noRangeThread = new Thread(NoRangeSend);
noRangeThread.Start(fsHelper);

            }
            else
            {
                //Probably a large file
               Thread rangeThread = new Thread(SendWithRange);
rangeThread.Start(fsHelper);

            }
        }

        //TODO: Finish implementation
        /// <summary>
        /// Sends entire file to requestor since it is small
        /// </summary>
        /// <param name="fsHelperObj">Helper object containing data relevant for thread execution</param>
        private void NoRangeSend(object fsHelperObj)
        {//Here we just send the file without using ranges and this function runs in it's own thread
            FileSenderHeler fsHelper = (FileSenderHeler)fsHelperObj;
            Socket handler = fsHelper.getSocket();
            String requestFile = fsHelper.getRequestFile();
            FileStream fsFile = null;
            long chunkSize = 50000;
            long bytesSent = 0;
        
            string ContentType = GetContentType(requestFile.ToLower());
            logger.LogDebug("Sending file: {0}", requestFile);

            if (!File.Exists(requestFile)) { handler.Close(); return; }
            FileInfo fInfo = new FileInfo(requestFile);
            if (fInfo.Length > 8000000)
                chunkSize = 500000;//Looks big like a movie so increase the chunk size
            string Reply = "HTTP/1.1 200 OK" + Environment.NewLine + "Server: VLC" + Environment.NewLine + "Content-Type: " + ContentType + Environment.NewLine;
            Reply += "Connection: close" + Environment.NewLine;
            Reply += "Content-Length: " + fInfo.Length + Environment.NewLine + Environment.NewLine;

            fsFile = new FileStream(requestFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            fsFile.Seek(0, SeekOrigin.Begin);

            handler.Send(UTF8Encoding.UTF8.GetBytes(Reply), SocketFlags.None);
            while (this.running && handler.Connected && chunkSize > 0)
            {
			
byte[] buffer = new byte[chunkSize];
int bytesRead;
while ((bytesRead = fsFile.Read(buffer, 0, buffer.Length)) > 0)
{
    handler.Send(buffer, bytesRead, SocketFlags.None);
}
            }
            fsFile.Close();
            CloseClientSocket(handler);
        }

        //TODO: Finish implementation
        /// <summary>
        /// Streams a movie to the requestors size they are too big to go all at once
        /// </summary>
        /// <param name="fsHelperObj">Helper object containing data relevant for thread execution</param>
        private void SendWithRange(object fsHelperObj)
        {//Streams a movie using ranges and runs on it's own thread
            FileSenderHeler fsHelper = (FileSenderHeler)fsHelperObj;
            Socket handler = fsHelper.getSocket();
            String requestFile = fsHelper.getRequestFile();

            logger.LogDebug("Streaming movie: {0}", requestFile);

            long chunkSize = 500000;
            long range = fsHelper.getRange(); //get range from the request
            long bytesSent = 0;
            long byteToSend = 1;
            
            
            string ContentType = GetContentType(requestFile.ToLower());
            FileInfo fInfo = new FileInfo(requestFile);
            long fileLength = fInfo.Length;
            FileStream fs = new FileStream(requestFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

            string reply = ContentString(range, ContentType, fileLength);
            handler.Send(UTF8Encoding.UTF8.GetBytes(reply), SocketFlags.None);
            byte[] buf = new byte[chunkSize];
            if (fs.CanSeek)
                fs.Seek(range, SeekOrigin.Begin);
            bytesSent = range;
            while (this.running && handler.Connected && byteToSend > 0)
            {
int bytesRead = fs.Read(buf, 0, buf.Length);
if (bytesRead <= 0) break;
handler.Send(buf, bytesRead, SocketFlags.None);
bytesSent += bytesRead;
byteToSend = fileLength - bytesSent;
            }
            if (!this.running) { try { fs.Close(); fs = null; } catch {; } }
            CloseClientSocket(handler);
        }

        //INSPIRED BY ORIGINAL PROJECT
        private string EncodeUrlPaths(string Value)
        {//Encode requests sent to the DLNA device
            if (Value == null) return null;
            return Value.Replace("%", "&percnt;").Replace("&", "&amp;").Replace("\\", "/");
        }

        //FROM ORIGINAL PROJECT
        private bool IsMusicOrImage(string fileName)
        {//We don't want to use byte-ranges for music or image data so we test the filename here
            if (fileName.ToLower().EndsWith(".jpg") || fileName.ToLower().EndsWith(".png") || fileName.ToLower().EndsWith(".gif") || fileName.ToLower().EndsWith(".mp3"))
                return true;
            return false;
        }

        //FROM ORIGINAL PROJECT
        private string GetContentType(string FileName)
        {//Based on the file type we create our content type for the reply to the TV/DLNA device
            string ContentType = "audio/mpeg";
            if (FileName.ToLower().EndsWith(".jpg")) ContentType = "image/jpg";
            else if (FileName.ToLower().EndsWith(".png")) ContentType = "image/png";
            else if (FileName.ToLower().EndsWith(".gif")) ContentType = "image/gif";
            else if (FileName.ToLower().EndsWith(".avi")) ContentType = "video/avi";
            if (FileName.ToLower().EndsWith(".mp4")) ContentType = "video/mp4";
            return ContentType;
        }

        //INSPIRED FROM ORIGINAL PROJECT
        private string GMTTime(DateTime Time)
        {//Covert date to GMT time/date
            string GMT = Time.ToString("ddd, dd MMM yyyy HH':'mm':'ss 'GMT'");
            return GMT;//Example "Sat, 25 Jan 2014 12:03:19 GMT";
        }

        //FROM ORIGINAL PROJECT
        private string ContentString(long Range, string ContentType, long FileLength)
        {//Builds up our HTTP reply string for byte-range requests
            string Reply = "";
            Reply = "HTTP/1.1 206 Partial Content" + Environment.NewLine + "Server: VLC" + Environment.NewLine + "Content-Type: " + ContentType + Environment.NewLine;
            Reply += "Accept-Ranges: bytes" + Environment.NewLine;
            Reply += "Date: " + GMTTime(DateTime.Now) + Environment.NewLine;
            if (Range == 0)
            {
                Reply += "Content-Length: " + FileLength + Environment.NewLine;
                Reply += "Content-Range: bytes 0-" + (FileLength - 1) + "/" + FileLength + Environment.NewLine;
            }
            else
            {
                Reply += "Content-Length: " + (FileLength - Range) + Environment.NewLine;
                Reply += "Content-Range: bytes " + Range + "-" + (FileLength - 1) + "/" + FileLength + Environment.NewLine;
            }
            return Reply + Environment.NewLine;
        }
    }
    }


