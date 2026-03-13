namespace HBS.Models;

public class SearchCriteria
{
    public DateTime CheckIn { get; set; }
    public DateTime CheckOut { get; set; }
    public RoomCategory? Category { get; set; }
}

public class BookingRequest
{
    public int RoomId { get; set; }
    public int CustomerId { get; set; }
    public DateTime CheckIn { get; set; }
    public DateTime CheckOut { get; set; }
    public int PointsToRedeem { get; set; }
}

public class BookingResult
{
    public int BookingId { get; set; }
    public int PointsEarned { get; set; }
    public decimal FinalPrice { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}
