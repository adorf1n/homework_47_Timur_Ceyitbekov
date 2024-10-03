using System;
using System.IO;
using System.Net;
using System.Text;
using Newtonsoft.Json;

public class HttpServer
{
    public void Start()
    {
        HttpListener listener = new HttpListener();
        listener.Prefixes.Add("http://localhost:7777/");
        listener.Start();

        Console.WriteLine("Server started...");

        while (true)
        {
            HttpListenerContext context = listener.GetContext();
            Process(context);
        }
    }

    private void Process(HttpListenerContext context)
    {
        string fileName = context.Request.RawUrl.TrimStart('/');
        string content = "";

        try
        {
            if (context.Request.HttpMethod == "POST" && fileName.Equals("showText.html", StringComparison.OrdinalIgnoreCase))
            {
                using (var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding))
                {
                    var body = reader.ReadToEnd();
                    var jsonData = JsonConvert.DeserializeObject<TextData>(body);

                    content = BuildTextHtml(jsonData.Text);
                }
            }
            else
            {
                content = "<h1>404 - Page Not Found</h1>";
            }

            byte[] htmlBytes = Encoding.UTF8.GetBytes(content);
            context.Response.ContentType = "text/html";
            context.Response.StatusCode = (int)HttpStatusCode.OK;
            context.Response.OutputStream.Write(htmlBytes, 0, htmlBytes.Length);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            var errorResponse = Encoding.UTF8.GetBytes("<h1>500 - Internal Server Error</h1>");
            context.Response.OutputStream.Write(errorResponse, 0, errorResponse.Length);
        }
        finally
        {
            context.Response.Close();
        }
    }

    private string BuildTextHtml(string text)
    {
        return $@"
        <html>
            <head><title>Text Output</title></head>
            <body><h1>{text}</h1></body>
        </html>";
    }
}

public class TextData
{
    public string Text { get; set; }
}
