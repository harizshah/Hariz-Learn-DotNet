using Microsoft.AspNetCore.DataProtection.KeyManagement; // (Not used here) usually for encryption key management
using System.Text.Json; // Needed for converting JSON text to/from C# objects

// Create a WebApplication builder — sets up configuration and dependencies
var builder = WebApplication.CreateBuilder(args);

// Build the WebApplication — prepares it to handle HTTP requests
var app = builder.Build();

// Main request pipeline: handles all incoming HTTP requests
app.Run(async (HttpContext context) =>
{
    // ----------- HANDLE GET REQUESTS -----------
    if (context.Request.Method == "GET")
    {
        // If the request path starts with "/"
        if (context.Request.Path.StartsWithSegments("/"))
        {
            // Respond with request details (method, URL, headers)
            await context.Response.WriteAsync($"The method is: {context.Request.Method}\r\n");
            await context.Response.WriteAsync($"The Url is: {context.Request.Path}\r\n");

            await context.Response.WriteAsync($"\r\nHeaders:\r\n");
            foreach (var key in context.Request.Headers.Keys)
            {
                await context.Response.WriteAsync($"{key}: {context.Request.Headers[key]}\r\n");
            }
        }
        // If the request is to "/employees"
        else if (context.Request.Path.StartsWithSegments("/employees"))
        {
            // Get all employees from the repository
            var employees = EmployeesRepository.GetEmployees();

            // Loop through and display each employee’s name and position
            foreach (var employee in employees)
            {
                await context.Response.WriteAsync($"{employee.Name}: {employee.Position}\r\n");
            }
        }
    }

    // ----------- HANDLE POST REQUESTS -----------
    else if (context.Request.Method == "POST")
    {
        // If the request path is "/employees"
        if (context.Request.Path.StartsWithSegments("/employees"))
        {
            // Read request body (contains new employee data in JSON)
            using var reader = new StreamReader(context.Request.Body);
            var body = await reader.ReadToEndAsync();

            // Convert JSON to Employee object
            var employee = JsonSerializer.Deserialize<Employee>(body);

            // Add new employee to the list
            EmployeesRepository.AddEmployee(employee);
        }
    }

    // ----------- HANDLE PUT REQUESTS -----------
    else if (context.Request.Method == "PUT")
    {
        if (context.Request.Path.StartsWithSegments("/employees"))
        {
            // Read updated employee data from request body
            using var reader = new StreamReader(context.Request.Body);
            var body = await reader.ReadToEndAsync();

            // Deserialize JSON to Employee object
            var employee = JsonSerializer.Deserialize<Employee>(body);

            // Try updating the employee
            var result = EmployeesRepository.UpdateEmployee(employee);

            // Return response based on success or failure
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

    // ----------- HANDLE DELETE REQUESTS -----------
    else if (context.Request.Method == "DELETE")
    {
        // If path is "/employees"
        if (context.Request.Path.StartsWithSegments("/employees"))
        {
            // Check if "id" query parameter is provided (e.g., /employees?id=2)
            if (context.Request.Query.ContainsKey("id"))
            {
                var id = context.Request.Query["id"];

                // Convert id from string to int
                if (int.TryParse(id, out int employeeId))
                {
                    // Try deleting employee with matching id
                    var result = EmployeesRepository.DeleteEmployee(employeeId);

                    // Respond based on delete result
                    if (result)
                    {
                        await context.Response.WriteAsync("Employee is deleted successfully.");
                    }
                    else
                    {
                        await context.Response.WriteAsync("Employee not found.");
                    }
                }
            }
        }
    }
});

// Start the web server
app.Run();


// ======================= REPOSITORY =======================
static class EmployeesRepository
{
    // In-memory list of employees (acts like a database)
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

    // Update employee info (match by Id)
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

    // Delete employee (match by Id)
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


// ======================= MODEL CLASS =======================
public class Employee
{
    // Employee properties
    public int Id { get; set; }
    public string Name { get; set; }
    public string Position { get; set; }
    public double Salary { get; set; }

    // Constructor for creating Employee objects
    public Employee(int id, string name, string position, double salary)
    {
        Id = id;
        Name = name;
        Position = position;
        Salary = salary;
    }
}
