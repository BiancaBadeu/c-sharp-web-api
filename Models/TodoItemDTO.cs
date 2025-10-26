namespace TodoApi.Models;

public class TodoItemDTO
{
    public long Id { get; set; }
    public string? Name { get; set; }
    public bool IsComplete { get; set; }
    public DateTime Created { get; set; }
    public DateTime? Updated { get; set; }
}
