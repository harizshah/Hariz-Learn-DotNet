using Microsoft.AspNetCore.DataProtection.KeyManagement; // (Unused) normally used for key encryption management
using System.Text.Json; // For converting JSON text to and from C# objects

// Create a new web application builder (sets up config & services)
var builder = WebApplication.CreateBuilder(args);

// Build the app (ready to handle HTTP requests)
var app = builder.Build();

// Handle all incoming HTTP requests here
app.Run(async (HttpContext context) =>
{
    // ========== GET REQUEST ==========
    if (context.Request.Method == "GET")
    {
        // If path is root "/"
        if (context.Request.Path.StartsWithSegments("/"))
        {
            // Print request info
            await context.Response.WriteAsync($"The method is: {context.Request.Method}\r\n");
            await context.Response.WriteAsync($"The Url is: {context.Request.Path}\r\n");

            // Print all HTTP headers
            await context.Response.WriteAsync($"\r\nHeaders:\r\n");
            foreach (var key in context.Request.Headers.Keys)
            {
                await context.Response.WriteAsync($"{key}: {context.Request.Headers[key]}\r\n");
            }
        }
        // If path starts with "/employees"
        else if (context.Request.Path.StartsWithSegments("/employees"))
        {
            // Get all employees from repository
            var employees = EmployeesRepository.GetEmployees();

            // Print name & position of each employee
            foreach (var employee in employees)
            {
                await context.Response.WriteAsync($"{employee.Name}: {employee.Position}\r\n");
            }
        }
    }

    // ========== POST REQUEST ==========
    else if (context.Request.Method == "POST")
    {
        if (context.Request.Path.StartsWithSegments("/employees"))
        {
            // Read the body as text (JSON data)
            using var reader = new StreamReader(context.Request.Body);
            var body = await reader.ReadToEndAsync();

            // Convert JSON text to Employee object
            var employee = JsonSerializer.Deserialize<Employee>(body);

            // Add new employee to in-memory list
            EmployeesRepository.AddEmployee(employee);
        }
    }

    // ========== PUT REQUEST ==========
    else if (context.Request.Method == "PUT")
    {
        if (context.Request.Path.StartsWithSegments("/employees"))
        {
            // Read body as JSON text
            using var reader = new StreamReader(context.Request.Body);
            var body = await reader.ReadToEndAsync();

            // Convert JSON to Employee object
            var employee = JsonSerializer.Deserialize<Employee>(body);

            // Update employee record
            var result = EmployeesRepository.UpdateEmployee(employee);

            // Send response back
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

    // ========== DELETE REQUEST ==========
    else if (context.Request.Method == "DELETE")
    {
        if (context.Request.Path.StartsWithSegments("/employees"))
        {
            // Check if the request contains a query parameter "id"
            if (context.Request.Query.ContainsKey("id"))
            {
                var id = context.Request.Query["id"];

                // Try to convert id from string to int
                if (int.TryParse(id, out int employeeId))
                {
                    // Check Authorization header
                    if (context.Request.Headers["Authorization"] == "frank")
                    {
                        // Try deleting employee
                        var result = EmployeesRepository.DeleteEmployee(employeeId);

                        // Respond based on result
                        if (result)
                        {
                            await context.Response.WriteAsync("Employee is deleted successfully.");
                        }
                        else
                        {
                            await context.Response.WriteAsync("Employee not found.");
                        }
                    }
                    else
                    {
                        // If Authorization header is wrong or missing
                        await context.Response.WriteAsync("You are not authorized to delete.");
                    }
                }
            }
        }
    }
});

// Run the web server (starts listening on localhost)
app.Run();


// ================== EMPLOYEE REPOSITORY ==================
static class EmployeesRepository
{
    // In-memory list of employees
    private static List<Employee> employees = new List<Employee>
    {
        new Employee(1, "John Doe", "Engineer", 60000),
        new Employee(2, "Jane Smith", "Manager", 75000),
        new Employee(3, "Sam Brown", "Technician", 50000)
    };

    // Return all employees
    public static List<Employee> GetEmployees() => employees;

    // Add new employee to list
    public static void AddEmployee(Employee? employee)
    {
        if (employee is not null)
        {
            employees.Add(employee);
        }
    }

    // Update an existing employee
    public static bool UpdateEmployee(Employee? employee)
    {
        if (employee is not null)
        {
            var emp = employees.FirstOrDefault(x => x.Id == employee.Id);
            if (emp is not null)
            {
                emp.Name = employee.Name;
                emp.Position = employee.Position;
                emp.Salary = employee.Salary;
                return true;
            }
        }
        return false;
    }

    // Delete employee by Id
    public static bool DeleteEmployee(int id)
    {
        var employee = employees.FirstOrDefault(x => x.Id == id);
        if (employee is not null)
        {
            employees.Remove(employee);
            return true;
        }
        return false;
    }
}


// ================== EMPLOYEE MODEL ==================
public class Employee
{
    // Employee properties
    public int Id { get; set; }
    public string Name { get; set; }
    public string Position { get; set; }
    public double Salary { get; set; }

    // Constructor to initialize new Employee
    public Employee(int id, string name, string position, double salary)
    {
        Id = id;
        Name = name;
        Position = position;
        Salary = salary;
    }
}
