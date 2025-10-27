using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TodoApi.Models;

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
    // <snippet_GetByID>
    [HttpGet("{id}")]
    public async Task<ActionResult<TodoItemDTO>> GetTodoItem(long id)
    {
        var todoItem = await _context.TodoItems.FindAsync(id);

        if (todoItem == null)
        {
            return NotFound();
        }

        return ItemToDTO(todoItem);
    }
    // </snippet_GetByID>

    // PUT: api/TodoItems/5
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    // <snippet_Update>
    [HttpPut("{id}")]
    public async Task<IActionResult> PutTodoItem(long id, TodoItemUpdateDTO updateDTO)
    {
        var todoItem = await _context.TodoItems.FindAsync(id);
        if (todoItem == null) return NotFound();
        
        if (!string.IsNullOrEmpty(updateDTO.Name)) 
            todoItem.Name = updateDTO.Name;
        
        todoItem.IsComplete = updateDTO.IsComplete;
        todoItem.UpdatedAt = DateTime.UtcNow;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException) when (!TodoItemExists(id))
        {
            return NotFound();
        }

        return Ok(ItemToDTO(todoItem));
    }
    // </snippet_Update>

    // POST: api/TodoItems
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    // <snippet_Create>
    [HttpPost]
    public async Task<ActionResult<TodoItemDTO>> PostTodoItem(TodoItemDTO todoDTO)
    {
        var todoItem = new TodoItem
        {
            Name = todoDTO.Name,
            IsComplete = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = null,
            Synchronized = false
        };

        _context.TodoItems.Add(todoItem);
        await _context.SaveChangesAsync();

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


        return CreatedAtAction(
            nameof(GetTodoItem),
            new { id = todoItem.Id },
            ItemToDTO(todoItem));
    }
    // </snippet_Create>

    // DELETE: api/TodoItems/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTodoItem(long id)
    {
        var todoItem = await _context.TodoItems.FindAsync(id);
        if (todoItem == null)
        {
            return NotFound();
        }

        _context.TodoItems.Remove(todoItem);
        await _context.SaveChangesAsync();

        return NoContent();
    }
    
    // GET: api/TodoItems/search?q=milk
    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<TodoItemDTO>>> SearchTodos([FromQuery] string q)
    {
        var results = await _context.TodoItems
            .Where(todo => todo.Name != null && todo.Name.ToLower().Contains(q.ToLower()))
            .Select(todo => ItemToDTO(todo))
            .ToListAsync();

        return Ok(results);
    }


    private bool TodoItemExists(long id)
    {
        return _context.TodoItems.Any(e => e.Id == id);
    }

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