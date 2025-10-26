using Microsoft.AspNetCore.DataProtection.KeyManagement; // (Unused here) — normally used for managing encryption keys
using System.Text.Json; // Used for JSON serialization/deserialization (convert between text and C# objects)

// Create a new WebApplication builder (configures app & dependencies)
var builder = WebApplication.CreateBuilder(args);

// Build the WebApplication instance
var app = builder.Build();

// Main middleware — handles every incoming HTTP request
app.Run(async (HttpContext context) =>
{
    // =============== ROOT PATH "/" ===============
    if (context.Request.Path.StartsWithSegments("/"))
    {
        // Return HTML response so browser displays formatted tags
        context.Response.Headers["Content-Type"] = "text/html";

        // Display request method and URL
        await context.Response.WriteAsync($"The method is: {context.Request.Method}<br/>");
        await context.Response.WriteAsync($"The Url is: {context.Request.Path}<br/>");

        // Display all headers as a bullet list
        await context.Response.WriteAsync($"<b>Headers</b>:<br/>");
        await context.Response.WriteAsync("<ul>");
        foreach (var key in context.Request.Headers.Keys)
        {
            await context.Response.WriteAsync($"<li><b>{key}</b>: {context.Request.Headers[key]}</li>");
        }
        await context.Response.WriteAsync("</ul>");
    }

    // =============== EMPLOYEE ROUTES "/employees" ===============
    else if (context.Request.Path.StartsWithSegments("/employees"))
    {
        // ---------- GET: Return all employees ----------
        if (context.Request.Method == "GET")
        {
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

        // ---------- POST: Add new employee ----------
        else if (context.Request.Method == "POST")
        {
            using var reader = new StreamReader(context.Request.Body);
            var body = await reader.ReadToEndAsync();

            try
            {
                // Convert JSON body into Employee object
                var employee = JsonSerializer.Deserialize<Employee>(body);

                // Validate input (ensure object not null and has valid Id)
                if (employee is null || employee.Id <= 0)
                {
                    context.Response.StatusCode = 400; // Bad Request
                    return;
                }

                // Add new employee to repository
                EmployeesRepository.AddEmployee(employee);

                context.Response.StatusCode = 201; // Created
                await context.Response.WriteAsync("Employee added successfully.");
            }
            catch (Exception ex)
            {
                // Handle invalid JSON or deserialization errors
                context.Response.StatusCode = 400; // Bad Request
                await context.Response.WriteAsync(ex.ToString());
                return;
            }
        }

        // ---------- PUT: Update existing employee ----------
        else if (context.Request.Method == "PUT")
        {
            using var reader = new StreamReader(context.Request.Body);
            var body = await reader.ReadToEndAsync();
            var employee = JsonSerializer.Deserialize<Employee>(body);

            var result = EmployeesRepository.UpdateEmployee(employee);

            if (result)
            {
                context.Response.StatusCode = 204; // No Content (success)
                await context.Response.WriteAsync("Employee updated successfully.");
                return;
            }
            else
            {
                context.Response.StatusCode = 404; // Not Found
                await context.Response.WriteAsync("Employee not found.");
            }
        }

        // ---------- DELETE: Remove employee ----------
        else if (context.Request.Method == "DELETE")
        {
            // Check if query contains ?id= parameter
            if (context.Request.Query.ContainsKey("id"))
            {
                var id = context.Request.Query["id"];

                // Validate id number
                if (int.TryParse(id, out int employeeId))
                {
                    // Basic authorization: requires header Authorization: frank
                    if (context.Request.Headers["Authorization"] == "frank")
                    {
                        var result = EmployeesRepository.DeleteEmployee(employeeId);

                        if (result)
                        {
                            await context.Response.WriteAsync("Employee is deleted successfully.");
                        }
                        else
                        {
                            context.Response.StatusCode = 404; // Not Found
                            await context.Response.WriteAsync("Employee not found.");
                        }
                    }
                    else
                    {
                        context.Response.StatusCode = 401; // Unauthorized
                        await context.Response.WriteAsync("You are not authorized to delete.");
                    }
                }
            }
        }
    }

    // =============== REDIRECTION ROUTE "/redirection" ===============
    else if (context.Request.Path.StartsWithSegments("/redirection"))
    {
        // Redirects user to the /employees page
        context.Response.Redirect("/employees");
    }

    // =============== UNKNOWN PATHS ===============
    else
    {
        // Return 404 Not Found for invalid routes
        context.Response.StatusCode = 404;
    }
});

// Start the web app and begin handling HTTP requests
app.Run();


// ====================== REPOSITORY CLASS ======================
static class EmployeesRepository
{
    // In-memory list of employees (acts like a temporary database)
    private static List<Employee> employees = new List<Employee>
    {
        new Employee(1, "John Doe", "Engineer", 60000),
        new Employee(2, "Jane Smith", "Manager", 75000),
        new Employee(3, "Sam Brown", "Technician", 50000)
    };

    // Return all employees
    public static List<Employee> GetEmployees() => employees;

    // Add a new employee
    public static void AddEmployee(Employee? employee)
    {
        if (employee is not null)
        {
            employees.Add(employee);
        }
    }

    // Update existing employee data
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


// ====================== EMPLOYEE MODEL ======================
public class Employee
{
    // Properties representing employee data
    public int Id { get; set; }
    public string Name { get; set; }
    public string Position { get; set; }
    public double Salary { get; set; }

    // Constructor for initialization
    public Employee(int id, string name, string position, double salary)
    {
        Id = id;
        Name = name;
        Position = position;
        Salary = salary;
    }
}
