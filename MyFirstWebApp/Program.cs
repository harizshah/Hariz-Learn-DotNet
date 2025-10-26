using Microsoft.AspNetCore.DataProtection.KeyManagement; // (Unused here) — usually for key management & encryption
using System.Text.Json; // Used to convert JSON text to/from C# objects

// Create a WebApplication builder — sets up configuration & dependencies
var builder = WebApplication.CreateBuilder(args);

// Build the app (finalizes configuration and prepares the HTTP pipeline)
var app = builder.Build();

// Main request handler — runs whenever an HTTP request hits the server
app.Run(async (HttpContext context) =>
{
    // ----------- ROOT PATH "/" -----------
    if (context.Request.Path.StartsWithSegments("/"))
    {
        // Print basic request info (method and URL)
        await context.Response.WriteAsync($"The method is: {context.Request.Method}\r\n");
        await context.Response.WriteAsync($"The Url is: {context.Request.Path}\r\n");

        // Print all request headers
        await context.Response.WriteAsync($"\r\nHeaders:\r\n");
        foreach (var key in context.Request.Headers.Keys)
        {
            await context.Response.WriteAsync($"{key}: {context.Request.Headers[key]}\r\n");
        }
    }

    // ----------- EMPLOYEES ROUTE "/employees" -----------
    else if (context.Request.Path.StartsWithSegments("/employees"))
    {
        // ===== GET /employees =====
        if (context.Request.Method == "GET")
        {
            // Retrieve all employees from repository
            var employees = EmployeesRepository.GetEmployees();

            // Display each employee's name and position
            foreach (var employee in employees)
            {
                await context.Response.WriteAsync($"{employee.Name}: {employee.Position}\r\n");
            }
        }

        // ===== POST /employees =====
        else if (context.Request.Method == "POST")
        {
            // Read request body (contains JSON data for new employee)
            using var reader = new StreamReader(context.Request.Body);
            var body = await reader.ReadToEndAsync();

            // Deserialize JSON into an Employee object
            var employee = JsonSerializer.Deserialize<Employee>(body);

            // Add the new employee to in-memory list
            EmployeesRepository.AddEmployee(employee);
        }

        // ===== PUT /employees =====
        else if (context.Request.Method == "PUT")
        {
            // Read body containing updated employee data
            using var reader = new StreamReader(context.Request.Body);
            var body = await reader.ReadToEndAsync();

            // Deserialize JSON into Employee object
            var employee = JsonSerializer.Deserialize<Employee>(body);

            // Try updating employee info
            var result = EmployeesRepository.UpdateEmployee(employee);

            // Return response message
            if (result)
                await context.Response.WriteAsync("Employee updated successfully.");
            else
                await context.Response.WriteAsync("Employee not found.");
        }

        // ===== DELETE /employees?id=1 =====
        else if (context.Request.Method == "DELETE")
        {
            // Check if query contains an "id" parameter
            if (context.Request.Query.ContainsKey("id"))
            {
                var id = context.Request.Query["id"];

                // Convert "id" from string to integer
                if (int.TryParse(id, out int employeeId))
                {
                    // Basic authorization check — must send header Authorization: frank
                    if (context.Request.Headers["Authorization"] == "frank")
                    {
                        // Attempt to delete the employee
                        var result = EmployeesRepository.DeleteEmployee(employeeId);

                        // Respond accordingly
                        if (result)
                            await context.Response.WriteAsync("Employee is deleted successfully.");
                        else
                            await context.Response.WriteAsync("Employee not found.");
                    }
                    else
                    {
                        // If Authorization header missing or incorrect
                        await context.Response.WriteAsync("You are not authorized to delete.");
                    }
                }
            }
        }
    }
});

// Start the web server (begins listening for HTTP requests)
app.Run();


// ======================= EMPLOYEE REPOSITORY =======================
static class EmployeesRepository
{
    // In-memory list of employees (mock database)
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
            employees.Add(employee);
    }

    // Update an existing employee (matched by Id)
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
                return true; // Update successful
            }
        }
        return false; // Employee not found
    }

    // Delete an employee (matched by Id)
    public static bool DeleteEmployee(int id)
    {
        var employee = employees.FirstOrDefault(x => x.Id == id);
        if (employee is not null)
        {
            employees.Remove(employee);
            return true; // Deletion successful
        }
        return false; // Employee not found
    }
}


// ======================= EMPLOYEE MODEL =======================
public class Employee
{
    // Employee properties
    public int Id { get; set; }
    public string Name { get; set; }
    public string Position { get; set; }
    public double Salary { get; set; }

    // Constructor to initialize Employee object
    public Employee(int id, string name, string position, double salary)
    {
        Id = id;
        Name = name;
        Position = position;
        Salary = salary;
    }
}
