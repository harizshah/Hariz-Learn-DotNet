using Microsoft.AspNetCore.DataProtection.KeyManagement; // (Not used here) — normally for data protection key management
using System.Text.Json; // Used for JSON serialization/deserialization

// Create a WebApplication builder — sets up configuration and services
var builder = WebApplication.CreateBuilder(args);

// Build the app — this finalizes configuration and prepares the app to handle requests
var app = builder.Build();

// The main request handler for all incoming HTTP requests
app.Run(async (HttpContext context) =>
{
    // ---------- HANDLE GET REQUEST ----------
    if (context.Request.Method == "GET")
    {
        // If request is made to root URL "/"
        if (context.Request.Path.StartsWithSegments("/"))
        {
            // Respond with the method and URL info
            await context.Response.WriteAsync($"The method is: {context.Request.Method}\r\n");
            await context.Response.WriteAsync($"The Url is: {context.Request.Path}\r\n");

            // Print all headers from the incoming request
            await context.Response.WriteAsync($"\r\nHeaders:\r\n");
            foreach (var key in context.Request.Headers.Keys)
            {
                await context.Response.WriteAsync($"{key}: {context.Request.Headers[key]}\r\n");
            }
        }
        // If request path starts with "/employees"
        else if (context.Request.Path.StartsWithSegments("/employees"))
        {
            // Get all employees from repository
            var employees = EmployeesRepository.GetEmployees();

            // Write each employee’s name and position to the response
            foreach (var employee in employees)
            {
                await context.Response.WriteAsync($"{employee.Name}: {employee.Position}\r\n");
            }
        }
    }

    // ---------- HANDLE POST REQUEST ----------
    else if (context.Request.Method == "POST")
    {
        // If path is "/employees"
        if (context.Request.Path.StartsWithSegments("/employees"))
        {
            // Read request body (JSON data)
            using var reader = new StreamReader(context.Request.Body);
            var body = await reader.ReadToEndAsync();

            // Convert JSON into Employee object
            var employee = JsonSerializer.Deserialize<Employee>(body);

            // Add new employee to the in-memory list
            EmployeesRepository.AddEmployee(employee);
        }
    }

    // ---------- HANDLE PUT REQUEST ----------
    else if (context.Request.Method == "PUT")
    {
        // If path is "/employees"
        if (context.Request.Path.StartsWithSegments("/employees"))
        {
            // Read updated employee JSON from request body
            using var reader = new StreamReader(context.Request.Body);
            var body = await reader.ReadToEndAsync();

            // Convert JSON text into Employee object
            var employee = JsonSerializer.Deserialize<Employee>(body);

            // Try updating the employee info
            var result = EmployeesRepository.UpdateEmployee(employee);

            // Respond with update status
            if (result)
            {
                await context.Response.WriteAsync("Employee updated successfully.");
            }
            else
            {
                await context.Response.WriteAsync("Employee not found.");
            }
        }
    }
});

// Start the application and listen for incoming HTTP requests
app.Run();


// ====================== REPOSITORY CLASS ======================
static class EmployeesRepository
{
    // In-memory data store (list of employees)
    private static List<Employee> employees = new List<Employee>
    {
        new Employee(1, "John Doe", "Engineer", 60000),
        new Employee(2, "Jane Smith", "Manager", 75000),
        new Employee(3, "Sam Brown", "Technician", 50000)
    };

    // Return all employees
    public static List<Employee> GetEmployees() => employees;

    // Add a new employee to the list
    public static void AddEmployee(Employee? employee)
    {
        if (employee is not null)
        {
            employees.Add(employee);
        }
    }

    // Update an existing employee (by matching Id)
    public static bool UpdateEmployee(Employee? employee)
    {
        if (employee is not null)
        {
            // Find existing employee by Id
            var emp = employees.FirstOrDefault(x => x.Id == employee.Id);
            if (emp is not null)
            {
                // Update fields
                emp.Name = employee.Name;
                emp.Position = employee.Position;
                emp.Salary = employee.Salary;

                return true; // Update succeeded
            }
        }

        return false; // Update failed
    }
}


// ====================== MODEL CLASS ======================
public class Employee
{
    // Properties for employee data
    public int Id { get; set; }
    public string Name { get; set; }
    public string Position { get; set; }
    public double Salary { get; set; }

    // Constructor to create Employee objects
    public Employee(int id, string name, string position, double salary)
    {
        Id = id;
        Name = name;
        Position = position;
        Salary = salary;
    }
}