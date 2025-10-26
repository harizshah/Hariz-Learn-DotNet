using Microsoft.AspNetCore.DataProtection.KeyManagement; // (Not used here) typically for key encryption management
using System.Text.Json; // Used for JSON serialization and deserialization

// Create a WebApplication builder (sets up services and configuration)
var builder = WebApplication.CreateBuilder(args);

// Build the web app (prepares it to handle HTTP requests)
var app = builder.Build();

// The main request handler — executes for every HTTP request
app.Run(async (HttpContext context) =>
{
    // --------- ROOT PATH "/" ----------
    if (context.Request.Path.StartsWithSegments("/"))
    {
        // Set response type to HTML so browser renders tags properly
        context.Response.Headers["Content-Type"] = "text/html";

        // Display basic request info
        await context.Response.WriteAsync($"The method is: {context.Request.Method}<br/>");
        await context.Response.WriteAsync($"The Url is: {context.Request.Path}<br/>");

        // Show all request headers in a list format
        await context.Response.WriteAsync($"<b>Headers</b>:<br/>");
        await context.Response.WriteAsync("<ul>");
        foreach (var key in context.Request.Headers.Keys)
        {
            await context.Response.WriteAsync($"<li><b>{key}</b>: {context.Request.Headers[key]}</li>");
        }
        await context.Response.WriteAsync("</ul>");
    }

    // --------- EMPLOYEE ROUTE "/employees" ----------
    else if (context.Request.Path.StartsWithSegments("/employees"))
    {
        // ===== GET: Retrieve all employees =====
        if (context.Request.Method == "GET")
        {
            var employees = EmployeesRepository.GetEmployees(); // Fetch from repository

            // Respond in HTML format
            context.Response.Headers["Content-Type"] = "text/html";
            await context.Response.WriteAsync("<ul>");
            foreach (var employee in employees)
            {
                await context.Response.WriteAsync($"<li><b>{employee.Name}</b>: {employee.Position}</li>");
            }
            await context.Response.WriteAsync("</ul>");

            context.Response.StatusCode = 200; // OK
        }

        // ===== POST: Add a new employee =====
        else if (context.Request.Method == "POST")
        {
            // Read JSON body from request
            using var reader = new StreamReader(context.Request.Body);
            var body = await reader.ReadToEndAsync();

            // Deserialize into Employee object
            var employee = JsonSerializer.Deserialize<Employee>(body);

            // Add employee to repository
            EmployeesRepository.AddEmployee(employee);

            context.Response.StatusCode = 201; // Created
            await context.Response.WriteAsync("Employee added successfully.");
        }

        // ===== PUT: Update existing employee =====
        else if (context.Request.Method == "PUT")
        {
            using var reader = new StreamReader(context.Request.Body);
            var body = await reader.ReadToEndAsync();
            var employee = JsonSerializer.Deserialize<Employee>(body);

            var result = EmployeesRepository.UpdateEmployee(employee);

            if (result)
            {
                context.Response.StatusCode = 204; // No Content (success, no data returned)
                await context.Response.WriteAsync("Employee updated successfully.");
                return; // Stop further execution
            }
            else
            {
                await context.Response.WriteAsync("Employee not found.");
            }
        }

        // ===== DELETE: Remove employee by ID =====
        else if (context.Request.Method == "DELETE")
        {
            // Check if the "id" query parameter exists
            if (context.Request.Query.ContainsKey("id"))
            {
                var id = context.Request.Query["id"];

                // Validate that id is a number
                if (int.TryParse(id, out int employeeId))
                {
                    // Check for simple authorization header
                    if (context.Request.Headers["Authorization"] == "frank")
                    {
                        // Try deleting employee
                        var result = EmployeesRepository.DeleteEmployee(employeeId);

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
                        // Unauthorized attempt
                        await context.Response.WriteAsync("You are not authorized to delete.");
                    }
                }
            }
        }
    }
});

// Start the web app and begin listening for requests
app.Run();


// ====================== REPOSITORY CLASS ======================
static class EmployeesRepository
{
    // In-memory list of employees (simulates a small database)
    private static List<Employee> employees = new List<Employee>
    {
        new Employee(1, "John Doe", "Engineer", 60000),
        new Employee(2, "Jane Smith", "Manager", 75000),
        new Employee(3, "Sam Brown", "Technician", 50000)
    };

    // Retrieve all employees
    public static List<Employee> GetEmployees() => employees;

    // Add new employee to list
    public static void AddEmployee(Employee? employee)
    {
        if (employee is not null)
        {
            employees.Add(employee);
        }
    }

    // Update an existing employee’s details
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
        return false; // Employee not found
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
        return false; // Not found
    }
}


// ====================== MODEL CLASS ======================
public class Employee
{
    // Employee data fields
    public int Id { get; set; }
    public string Name { get; set; }
    public string Position { get; set; }
    public double Salary { get; set; }

    // Constructor to initialize Employee
    public Employee(int id, string name, string position, double salary)
    {
        Id = id;
        Name = name;
        Position = position;
        Salary = salary;
    }
}
