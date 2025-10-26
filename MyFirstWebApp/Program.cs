using Microsoft.AspNetCore.DataProtection.KeyManagement; // (Unused) — usually for encryption key management
using Microsoft.AspNetCore.Http; // Gives access to HttpContext, Request, and Response
using System.Text.Json; // For JSON serialization/deserialization (convert between JSON text and C# objects)

// Create a new web app builder — sets up app configuration
var builder = WebApplication.CreateBuilder(args);

// Build the web app — prepares it to handle requests
var app = builder.Build();

// Main middleware that handles all incoming HTTP requests
app.Run(async (HttpContext context) =>
{
    // =============== ROOT PATH "/" ===============
    if (context.Request.Path.StartsWithSegments("/"))
    {
        // Set response to HTML
        context.Response.Headers["Content-Type"] = "text/html";

        // Show request method and URL
        await context.Response.WriteAsync($"The method is: {context.Request.Method}<br/>");
        await context.Response.WriteAsync($"The Url is: {context.Request.Path}<br/>");

        // List all headers
        await context.Response.WriteAsync($"<b>Headers</b>:<br/>");
        await context.Response.WriteAsync("<ul>");
        foreach (var key in context.Request.Headers.Keys)
        {
            await context.Response.WriteAsync($"<li><b>{key}</b>: {context.Request.Headers[key]}</li>");
        }
        await context.Response.WriteAsync("</ul>");
    }

    // =============== EMPLOYEES ROUTE "/employees" ===============
    else if (context.Request.Path.StartsWithSegments("/employees"))
    {
        // ---------- GET ----------
        if (context.Request.Method == "GET")
        {
            // Check if query includes an ID (e.g. /employees?id=2)
            if (context.Request.Query.ContainsKey("id"))
            {
                var id = context.Request.Query["id"];

                // Convert id to integer
                if (int.TryParse(id, out int employeeId))
                {
                    // Retrieve one specific employee by ID
                    var employee = EmployeesRepository.GetEmployeeById(employeeId);

                    context.Response.ContentType = "text/html";

                    if (employee is not null)
                    {
                        // Show specific employee details
                        await context.Response.WriteAsync($"Name: {employee.Name}<br/>");
                        await context.Response.WriteAsync($"Position: {employee.Position}<br/>");
                        await context.Response.WriteAsync($"Salary: {employee.Salary}<br/>");
                    }
                    else
                    {
                        // Employee not found
                        context.Response.StatusCode = 404;
                        await context.Response.WriteAsync("Employee not found.");
                    }
                }
            }
            else
            {
                // No ID → show all employees
                var employees = EmployeesRepository.GetEmployees();

                context.Response.Headers["Content-Type"] = "text/html";
                await context.Response.WriteAsync("<ul>");
                foreach (var employee in employees)
                {
                    await context.Response.WriteAsync($"<li><b>{employee.Name}</b>: {employee.Position}</li>");
                }
                await context.Response.WriteAsync("</ul>");
                context.Response.StatusCode = 200; // OK
            }
        }

        // ---------- POST ----------
        else if (context.Request.Method == "POST")
        {
            using var reader = new StreamReader(context.Request.Body);
            var body = await reader.ReadToEndAsync();

            try
            {
                // Convert request body JSON to Employee object
                var employee = JsonSerializer.Deserialize<Employee>(body);

                // Validate employee data
                if (employee is null || employee.Id <= 0)
                {
                    context.Response.StatusCode = 400; // Bad Request
                    return;
                }

                // Add new employee
                EmployeesRepository.AddEmployee(employee);

                context.Response.StatusCode = 201; // Created
                await context.Response.WriteAsync("Employee added successfully.");
            }
            catch (Exception ex)
            {
                // Handle invalid JSON
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync(ex.ToString());
                return;
            }
        }

        // ---------- PUT ----------
        else if (context.Request.Method == "PUT")
        {
            using var reader = new StreamReader(context.Request.Body);
            var body = await reader.ReadToEndAsync();

            // Deserialize the updated employee info
            var employee = JsonSerializer.Deserialize<Employee>(body);
            var result = EmployeesRepository.UpdateEmployee(employee);

            if (result)
            {
                context.Response.StatusCode = 204; // No Content (update success)
                await context.Response.WriteAsync("Employee updated successfully.");
                return;
            }
            else
            {
                // If no employee with the given ID
                await context.Response.WriteAsync("Employee not found.");
            }
        }

        // ---------- DELETE ----------
        else if (context.Request.Method == "DELETE")
        {
            // Check for query ?id=
            if (context.Request.Query.ContainsKey("id"))
            {
                var id = context.Request.Query["id"];

                if (int.TryParse(id, out int employeeId))
                {
                    // Simple authorization: must include header Authorization: frank
                    if (context.Request.Headers["Authorization"] == "frank")
                    {
                        var result = EmployeesRepository.DeleteEmployee(employeeId);

                        if (result)
                        {
                            await context.Response.WriteAsync("Employee is deleted successfully.");
                        }
                        else
                        {
                            context.Response.StatusCode = 404;
                            await context.Response.WriteAsync("Employee not found.");
                        }
                    }
                    else
                    {
                        // Unauthorized
                        context.Response.StatusCode = 401;
                        await context.Response.WriteAsync("You are not authorized to delete.");
                    }
                }
            }
        }
    }

    // =============== REDIRECTION ROUTE "/redirection" ===============
    else if (context.Request.Path.StartsWithSegments("/redirection"))
    {
        // Redirects user to /employees
        context.Response.Redirect("/employees");
    }

    // =============== DEFAULT: NOT FOUND ===============
    else
    {
        // For all other paths
        context.Response.StatusCode = 404;
    }
});

// Start the web server
app.Run();


// ==================== EMPLOYEE REPOSITORY ====================
static class EmployeesRepository
{
    // In-memory database (list of employees)
    private static List<Employee> employees = new List<Employee>
    {
        new Employee(1, "John Doe", "Engineer", 60000),
        new Employee(2, "Jane Smith", "Manager", 75000),
        new Employee(3, "Sam Brown", "Technician", 50000)
    };

    // Get all employees
    public static List<Employee> GetEmployees() => employees;

    // Get one employee by ID
    public static Employee? GetEmployeeById(int id)
    {
        return employees.FirstOrDefault(x => x.Id == id);
    }

    // Add new employee
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

    // Delete employee by ID
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


// ==================== EMPLOYEE CLASS (MODEL) ====================
public class Employee
{
    // Properties (columns)
    public int Id { get; set; }
    public string Name { get; set; }
    public string Position { get; set; }
    public double Salary { get; set; }

    // Constructor to initialize Employee data
    public Employee(int id, string name, string position, double salary)
    {
        Id = id;
        Name = name;
        Position = position;
        Salary = salary;
    }
}
