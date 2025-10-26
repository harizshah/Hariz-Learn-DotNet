using Microsoft.AspNetCore.DataProtection.KeyManagement; 
// Imports a namespace (though not used here). Typically for managing encryption keys in ASP.NET Core.

// Create a WebApplication builder — this sets up the app configuration and services
var builder = WebApplication.CreateBuilder(args);

// Build the WebApplication — creates the app instance to handle HTTP requests
var app = builder.Build();

// Define what happens when a request comes to the server
app.Run(async (HttpContext context) =>
{
    // Check if the incoming HTTP request is a GET request
    if (context.Request.Method == "GET")
    {
        // If the request path starts with "/" (the root)
        if (context.Request.Path.StartsWithSegments("/"))
        {
            // Write basic request info back to the browser
            await context.Response.WriteAsync($"The method is: {context.Request.Method}\r\n");
            await context.Response.WriteAsync($"The Url is: {context.Request.Path}\r\n");

            // Print all headers from the request
            await context.Response.WriteAsync($"\r\nHeaders:\r\n");
            foreach (var key in context.Request.Headers.Keys)
            {
                await context.Response.WriteAsync($"{key}: {context.Request.Headers[key]}\r\n");
            }
        }
        // If the URL path starts with "/employees"
        else if (context.Request.Path.StartsWithSegments("/employees"))
        {
            // Get the list of employees from the repository
            var employees = EmployeesRepository.GetEmployees();

            // Loop through the list and print each employee's name and position
            foreach (var employee in employees) 
            {
                await context.Response.WriteAsync($"{employee.Name}: {employee.Position}\r\n");
            }
        }
    }
});

// Start the app and listen for HTTP requests
app.Run();


// ---------------- Supporting classes below ----------------

// Static class to store and provide employee data
static class EmployeesRepository
{
    // List of employees stored in memory
    private static List<Employee> employees = new List<Employee>
    {
        new Employee(1, "John Doe", "Engineer", 60000),
        new Employee(2, "Jane Smith", "Manager", 75000),
        new Employee(3, "Sam Brown", "Technician", 50000)
    };

    // Public method to return the list of employees
    public static List<Employee> GetEmployees() => employees;
}

// Employee class represents each employee object
public class Employee
{
    // Properties for employee details
    public int Id { get; set; }
    public string Name { get; set; }
    public string Position { get; set; }
    public double Salary { get; set; }

    // Constructor to initialize employee details
    public Employee(int id, string name, string position, double salary)
    {
        Id = id;
        Name = name;
        Position = position;
        Salary = salary;
    }
}
