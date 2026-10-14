using Microsoft.AspNetCore.Mvc;
using GigaTestApp.Models;

namespace GigaTestApp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EmployeesController : ControllerBase
{
    private static readonly Dictionary<int, Employee> _employees = new();
    private static int _nextId = 1;

    private static readonly string[] _departments =
    [
        "Engineering", "Marketing", "Sales", "HR", "Finance", "Operations"
    ];

    private static readonly string[] _firstNames =
    [
        "Alex", "Maria", "Ivan", "Elena", "Dmitry", "Anna", "Sergey", "Olga",
        "Andrew", "Natalia", "Maxim", "Irina", "Vladimir", "Tatiana", "Pavel"
    ];

    private static readonly string[] _lastNames =
    [
        "Smith", "Johnson", "Ivanov", "Petrov", "Sidorov", "Kuznetsov",
        "Popov", "Sokolov", "Mikhailov", "Fedorov", "Morozov", "Volkov"
    ];

    [HttpGet]
    public IActionResult GetAll()
    {
        var employees = _employees.Values.OrderBy(e => e.Id).ToList();
        return Ok(employees);
    }

    [HttpGet("{id}")]
    public IActionResult GetById(int id)
    {
        if (!_employees.TryGetValue(id, out var employee))
            return NotFound(new { message = $"Employee with id {id} not found." });

        return Ok(employee);
    }

    [HttpGet("department/{department}")]
    public IActionResult GetByDepartment(string department)
    {
        var employees = _employees.Values
            .Where(e => e.Department.Equals(department, StringComparison.OrdinalIgnoreCase))
            .OrderBy(e => e.LastName)
            .ToList();

        return Ok(employees);
    }

    [HttpPost]
    public IActionResult Create([FromBody] Employee employee)
    {
        if (string.IsNullOrWhiteSpace(employee.FirstName) ||
            string.IsNullOrWhiteSpace(employee.LastName) ||
            string.IsNullOrWhiteSpace(employee.Email))
        {
            return BadRequest(new { message = "FirstName, LastName, and Email are required." });
        }

        employee.Id = _nextId++;
        employee.Department = string.IsNullOrWhiteSpace(employee.Department)
            ? _departments[Random.Shared.Next(_departments.Length)]
            : employee.Department;

        _employees[employee.Id] = employee;

        return CreatedAtAction(nameof(GetById), new { id = employee.Id }, employee);
    }

    [HttpPut("{id}")]
    public IActionResult Update(int id, [FromBody] Employee employee)
    {
        if (!_employees.ContainsKey(id))
            return NotFound(new { message = $"Employee with id {id} not found." });

        employee.Id = id;
        _employees[id] = employee;

        return Ok(employee);
    }

    [HttpDelete("{id}")]
    public IActionResult Delete(int id)
    {
        if (_employees.Remove(id))
            return NoContent();

        return NotFound(new { message = $"Employee with id {id} not found." });
    }

    [HttpPost("seed")]
    public IActionResult SeedData()
    {
        var random = Random.Shared;
        _employees.Clear();
        _nextId = 1;

        for (int i = 0; i < 20; i++)
        {
            var firstName = _firstNames[random.Next(_firstNames.Length)];
            var lastName = _lastNames[random.Next(_lastNames.Length)];

            _employees[_nextId++] = new Employee
            {
                FirstName = firstName,
                LastName = lastName,
                Email = $"{firstName.ToLower()}.{lastName.ToLower()}@company.com",
                Department = _departments[random.Next(_departments.Length)],
                Salary = Math.Round((decimal)(50000 + random.NextDouble() * 100000), 2),
                HireDate = DateOnly.FromDateTime(DateTime.Now.AddYears(-random.Next(0, 15)))
            };
        }

        return Ok(new { message = "20 employees generated.", count = _employees.Count });
    }
}
