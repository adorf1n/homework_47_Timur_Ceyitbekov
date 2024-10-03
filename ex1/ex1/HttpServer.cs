using System;
using System.IO;
using System.Net;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using RazorEngine;
using RazorEngine.Templating;

public class HttpServer
{
    private string _siteDir = Path.Combine(Directory.GetCurrentDirectory(), "Views");

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
            if (fileName.Contains("showEmployees.html"))
            {
                var query = context.Request.QueryString;
                int idFrom = string.IsNullOrEmpty(query["IdFrom"]) ? 0 : int.Parse(query["IdFrom"]);
                int idTo = string.IsNullOrEmpty(query["IdTo"]) ? int.MaxValue : int.Parse(query["IdTo"]);

                content = BuildEmployeeHtml(idFrom, idTo);
            }
            else
            {
                if (fileName.Contains("html"))
                {
                    content = BuildHtml(fileName);
                }
                else if (File.Exists(fileName))
                {
                    content = File.ReadAllText(fileName);
                }
                else
                {
                    content = "<h1>404 - Page Not Found</h1>";
                }
            }

            byte[] htmlBytes = System.Text.Encoding.UTF8.GetBytes(content);
            Stream fileStream = new MemoryStream(htmlBytes);

            context.Response.ContentType = "text/html";
            context.Response.StatusCode = (content != null) ? (int)HttpStatusCode.OK : (int)HttpStatusCode.NotFound;
            fileStream.CopyTo(context.Response.OutputStream);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            var errorResponse = System.Text.Encoding.UTF8.GetBytes("<h1>500 - Internal Server Error</h1>");
            context.Response.OutputStream.Write(errorResponse, 0, errorResponse.Length);
        }
        finally
        {
            context.Response.Close();
        }
    }

    private string BuildHtml(string filename)
    {
        string layoutPath = Path.Combine(_siteDir, "Shared", "layout.html");
        string filePath = Path.Combine(_siteDir, "Pages", filename);

        var razorService = Engine.Razor;

        if (!razorService.IsTemplateCached("layout", null))
            razorService.AddTemplate("layout", File.ReadAllText(layoutPath));

        if (!razorService.IsTemplateCached(filename, null))
        {
            razorService.AddTemplate(filename, File.ReadAllText(filePath));
            razorService.Compile(filename);
        }

        string html = razorService.Run(filename, null, new
        {
            IndexTitle = "My Index title",
            Page1 = "My Page1",
            Page2 = "My Page2",
            Page3 = "My Page3",
            X = -1
        });

        return html;
    }

    private string BuildEmployeeHtml(int idFrom, int idTo)
    {
        string filePath = Path.Combine(_siteDir, "../../..", "Data", "employees.json");
        var employees = JsonConvert.DeserializeObject<List<Employee>>(File.ReadAllText(filePath));

        var filteredEmployees = employees
            .Where(e => e.ID >= idFrom && e.ID <= idTo)
            .OrderBy(e => e.ID)
            .ToList();

        string layoutPath = Path.Combine(_siteDir, "Pages", "employeeLayout.html");

        AddTemplateIfNotCached("employeeLayout", layoutPath);

        var razorService = Engine.Razor;
        string html = razorService.Run("employeeLayout", null, new { Employees = filteredEmployees });

        return html;
    }

    private void AddTemplateIfNotCached(string templateName, string templatePath)
    {
        var razorService = Engine.Razor;

        if (!razorService.IsTemplateCached(templateName, null))
        {
            try
            {
                if (File.Exists(templatePath))
                {
                    Console.WriteLine($"Reading template from: {templatePath}");
                    var content = File.ReadAllText(templatePath);
                    razorService.AddTemplate(templateName, content);
                    razorService.Compile(templateName); 
                    Console.WriteLine($"Template '{templateName}' added and compiled.");
                }
                else
                {
                    Console.WriteLine($"Template file does not exist: {templatePath}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading template {templateName}: {ex.Message}");
            }
        }
    }
}

public class Employee
{
    public int ID { get; set; }
    public string Name { get; set; }
    public int Age { get; set; }
}
