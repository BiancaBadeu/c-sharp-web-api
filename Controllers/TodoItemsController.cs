using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TodoApi.Models;
using System.Net.Http.Json;

namespace TodoApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class TodoItemsController : ControllerBase
{
    private readonly TodoContext _context;

    public TodoItemsController(TodoContext context)
    {
        _context = context;
    }

    // GET: api/TodoItems
    [HttpGet]
    public async Task<ActionResult<IEnumerable<TodoItemDTO>>> GetTodoItems()
    {
        return await _context.TodoItems
            .Select(x => ItemToDTO(x))
            .ToListAsync();
    }

    // GET: api/TodoItems/5
    [HttpGet("{id}")]
    public async Task<ActionResult<TodoItemDTO>> GetTodoItem(long id)
    {
        var todoItem = await _context.TodoItems.FindAsync(id);
        if (todoItem == null) return NotFound();
        return ItemToDTO(todoItem);
    }

    // PUT: api/TodoItems/5
    [HttpPut("{id}")]
    public async Task<ActionResult<TodoItemDTO>> PutTodoItem(long id, TodoItemUpdateDTO updateDTO)
    {
        var todoItem = await _context.TodoItems.FindAsync(id);
        if (todoItem == null) return NotFound();

        // Only update fields that are not null (optional)
        if (updateDTO.Name != null)
            todoItem.Name = updateDTO.Name;

        todoItem.IsComplete = updateDTO.IsComplete;
        todoItem.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(ItemToDTO(todoItem));
    }

    // POST: api/TodoItems
    [HttpPost]
    public async Task<ActionResult<TodoItemDTO>> PostTodoItem(TodoItemDTO todoDTO)
    {
        var todoItem = new TodoItem
        {
            Name = todoDTO.Name,
            IsComplete = todoDTO.IsComplete,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Synchronized = false
        };

        _context.TodoItems.Add(todoItem);
        await _context.SaveChangesAsync();

        // Attempt to send to Postman Echo
        try
        {
            using var client = new HttpClient();
            var response = await client.PostAsJsonAsync("https://postman-echo.com/post", todoItem);
            if (response.IsSuccessStatusCode)
            {
                todoItem.Synchronized = true;
                await _context.SaveChangesAsync();
            }
        }
        catch
        {
            todoItem.Synchronized = false;
        }

        return CreatedAtAction(nameof(GetTodoItem),
            new { id = todoItem.Id },
            ItemToDTO(todoItem));
    }

    // DELETE: api/TodoItems/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTodoItem(long id)
    {
        var todoItem = await _context.TodoItems.FindAsync(id);
        if (todoItem == null) return NotFound();

        _context.TodoItems.Remove(todoItem);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // GET: api/TodoItems/search?q=keyword
    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<TodoItemDTO>>> SearchTodos([FromQuery] string q)
    {
        var results = await _context.TodoItems
            .Where(x => x.Name != null && x.Name.ToLower().Contains(q.ToLower()))
            .Select(x => ItemToDTO(x))
            .ToListAsync();

        return Ok(results);
    }

    private bool TodoItemExists(long id) =>
        _context.TodoItems.Any(e => e.Id == id);

    private static TodoItemDTO ItemToDTO(TodoItem todoItem) =>
        new TodoItemDTO
        {
            Id = todoItem.Id,
            Name = todoItem.Name,
            IsComplete = todoItem.IsComplete,
            CreatedAt = todoItem.CreatedAt,
            UpdatedAt = todoItem.UpdatedAt,
            Synchronized = todoItem.Synchronized
        };
}
