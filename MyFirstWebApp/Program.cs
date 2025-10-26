using Microsoft.AspNetCore.DataProtection.KeyManagement; // Namespace (not used here, but for managing encryption keys)
using System.Text.Json; // Used to convert JSON text into C# objects and vice versa

// Create a web app builder (sets up configuration, services, etc.)
var builder = WebApplication.CreateBuilder(args);

// Build the app (creates the request handling pipeline)
var app = builder.Build();

// Define the main request handler
app.Run(async (HttpContext context) =>
{
    // Handle GET requests
    if (context.Request.Method == "GET")
    {
        // If the request path starts with "/" (root URL)
        if (context.Request.Path.StartsWithSegments("/"))
        {
            // Respond with method and URL
            await context.Response.WriteAsync($"The method is: {context.Request.Method}\r\n");
            await context.Response.WriteAsync($"The Url is: {context.Request.Path}\r\n");

            // Print all headers from the request
            await context.Response.WriteAsync($"\r\nHeaders:\r\n");
            foreach (var key in context.Request.Headers.Keys)
            {
                await context.Response.WriteAsync($"{key}: {context.Request.Headers[key]}\r\n");
            }
        }
        // If the request path is "/employees"
        else if (context.Request.Path.StartsWithSegments("/employees"))
        {
            // Get all employees from the repository
            var employees = EmployeesRepository.GetEmployees();

            // Loop through employees and print name + position
            foreach (var employee in employees)
            {
                await context.Response.WriteAsync($"{employee.Name}: {employee.Position}\r\n");
            }
        }
    }
    // Handle POST requests
    else if (context.Request.Method == "POST")
    {
        // If the request path is "/employees"
        if (context.Request.Path.StartsWithSegments("/employees"))
        {
            // Read the JSON body from the request
            using var reader = new StreamReader(context.Request.Body);
            var body = await reader.ReadToEndAsync();

            // Convert JSON text into an Employee object
            var employee = JsonSerializer.Deserialize<Employee>(body);

            // Add the new employee to the list
            EmployeesRepository.AddEmployee(employee);
        }
    }
});

// Run the web application (start listening for requests)
app.Run();


// ---------------- Repository Class ----------------
static class EmployeesRepository
{
    // Static list of employees in memory
    private static List<Employee> employees = new List<Employee>
    {
        new Employee(1, "John Doe", "Engineer", 60000),
        new Employee(2, "Jane Smith", "Manager", 75000),
        new Employee(3, "Sam Brown", "Technician", 50000)
    };

    // Returns all employees
    public static List<Employee> GetEmployees() => employees;

    // Adds a new employee to the list (if not null)
    public static void AddEmployee(Employee? employee)
    {
        if (employee is not null)
        {
            employees.Add(employee);
        }
    }
}


// ---------------- Model Class ----------------
public class Employee
{
    // Properties of Employee
    public int Id { get; set; }
    public string Name { get; set; }
    public string Position { get; set; }
    public double Salary { get; set; }

    // Constructor to initialize a new Employee
    public Employee(int id, string name, string position, double salary)
    {
        Id = id;
        Name = name;
        Position = position;
        Salary = salary;
    }
}
