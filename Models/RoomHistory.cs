namespace HBS.Models;

public class RoomHistory
{
    public int Id { get; set; }
    public int RoomId { get; set; }
    public DateTime Timestamp { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int? CustomerId { get; set; }
}
