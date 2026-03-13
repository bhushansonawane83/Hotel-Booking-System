using HBS.Models;

namespace HBS.Services;

public class HotelService : IHotelService
{
    private readonly List<Room> _rooms = new();
    private readonly List<Booking> _bookings = new();
    private readonly List<Customer> _customers = new();
    private readonly List<RoomHistory> _roomHistory = new();
    private readonly DataProtectionService _dataProtection;
    private int _nextBookingId = 1;
    private int _nextCustomerId = 1;
    private int _nextHistoryId = 1;

    public HotelService(DataProtectionService dataProtection)
    {
        _dataProtection = dataProtection;
        SeedData();
    }

    private void SeedData()
    {
        _rooms.AddRange(new[]
        {
            new Room { Id = 101, RoomNumber = "101", Category = RoomCategory.Single, PricePerNight = 80, Capacity = 1 },
            new Room { Id = 102, RoomNumber = "102", Category = RoomCategory.Single, PricePerNight = 80, Capacity = 1 },
            new Room { Id = 201, RoomNumber = "201", Category = RoomCategory.Double, PricePerNight = 120, Capacity = 2 },
            new Room { Id = 202, RoomNumber = "202", Category = RoomCategory.Double, PricePerNight = 120, Capacity = 2 },
            new Room { Id = 203, RoomNumber = "203", Category = RoomCategory.Double, PricePerNight = 120, Capacity = 2 },
            new Room { Id = 301, RoomNumber = "301", Category = RoomCategory.Suite, PricePerNight = 200, Capacity = 4 },
            new Room { Id = 401, RoomNumber = "401", Category = RoomCategory.Deluxe, PricePerNight = 300, Capacity = 2 }
        });

        _customers.Add(new Customer 
        { 
            Id = 1, 
            Name = _dataProtection.Encrypt("Bhushan Sonawane"), 
            Email = _dataProtection.Encrypt("bhushan659@gmail.com"), 
            Phone = _dataProtection.Encrypt("96750629"), 
            LoyaltyPoints = 1200, 
            LoyaltyTier = "Gold" 
        });
        _nextCustomerId = 2;
    }

    public List<Room> GetAvailableRooms(DateTime checkIn, DateTime checkOut, RoomCategory? category = null)
    {
        var bookedRoomIds = _bookings
            .Where(b => b.Status != BookingStatus.Cancelled && 
                       !(b.CheckOut <= checkIn || b.CheckIn >= checkOut))
            .Select(b => b.RoomId)
            .ToHashSet();

        return _rooms
            .Where(r => !bookedRoomIds.Contains(r.Id) && 
                       (category == null || r.Category == category))
            .ToList();
    }
    public bool IsRoomAvailable(int roomId, DateTime checkIn, DateTime checkOut)
    {
        return !_bookings.Any(b =>
            b.RoomId == roomId &&
            b.Status != BookingStatus.Cancelled &&
            !(b.CheckOut <= checkIn || b.CheckIn >= checkOut));
    }

    public Room? GetRoom(int roomId) => _rooms.FirstOrDefault(r => r.Id == roomId);

    public List<Room> GetAllRooms() => _rooms;

    public Booking CreateBooking(int roomId, int customerId, DateTime checkIn, DateTime checkOut, int pointsToRedeem = 0)
    {
        var room = GetRoom(roomId);
        if (room == null) throw new Exception("Room not found");

        bool available = IsRoomAvailable(roomId, checkIn, checkOut);

        if (!available)
        {
            throw new Exception("Room alrady booked found");
        }
       

        var nights = (checkOut - checkIn).Days;
        var totalPrice = room.PricePerNight * nights;
        var discount = pointsToRedeem / 10m;
        var finalPrice = totalPrice - discount;

        var booking = new Booking
        {
            Id = _nextBookingId++,
            RoomId = roomId,
            CustomerId = customerId,
            CheckIn = checkIn,
            CheckOut = checkOut,
            TotalPrice = finalPrice,
            Status = BookingStatus.Confirmed,
            CreatedAt = DateTime.Now,
            Notes = pointsToRedeem > 0 ? $"Redeemed {pointsToRedeem} points for ${discount} discount" : null
        };

        _bookings.Add(booking);
        AddHistory(roomId, "Booking Created", $"Booking #{booking.Id} created", customerId);
        
        var customer = _customers.FirstOrDefault(c => c.Id == customerId);
        if (customer != null)
        {
            customer.LoyaltyPoints -= pointsToRedeem;
            customer.LoyaltyPoints += (int)(finalPrice / 10);
            UpdateLoyaltyTier(customer);
        }

        return booking;
    }

    public int CreateCustomer(string name, string email, string phone)
    {
        var customer = new Customer
        {
            Id = _nextCustomerId++,
            Name = _dataProtection.Encrypt(name),
            Email = _dataProtection.Encrypt(email),
            Phone = _dataProtection.Encrypt(phone),
            LoyaltyPoints = 0,
            LoyaltyTier = "Bronze"
        };
        _customers.Add(customer);
        return customer.Id;
    }

    public Customer? GetCustomer(int id)
    {
        var customer = _customers.FirstOrDefault(c => c.Id == id);
        if (customer != null)
        {
            return new Customer
            {
                Id = customer.Id,
                Name = _dataProtection.Decrypt(customer.Name),
                Email = _dataProtection.MaskEmail(_dataProtection.Decrypt(customer.Email)),
                Phone = _dataProtection.MaskPhone(_dataProtection.Decrypt(customer.Phone)),
                LoyaltyPoints = customer.LoyaltyPoints,
                LoyaltyTier = customer.LoyaltyTier
            };
        }
        return null;
    }

    public Customer? GetCustomerByEmail(string email) 
    {
        var customer = _customers.FirstOrDefault(c => 
            _dataProtection.Decrypt(c.Email).Equals(email, StringComparison.OrdinalIgnoreCase));
        
        if (customer != null)
        {
            return new Customer
            {
                Id = customer.Id,
                Name = _dataProtection.Decrypt(customer.Name),
                Email = _dataProtection.Decrypt(customer.Email),
                Phone = _dataProtection.Decrypt(customer.Phone),
                LoyaltyPoints = customer.LoyaltyPoints,
                LoyaltyTier = customer.LoyaltyTier
            };
        }
        return null;
    }

    public List<Booking> GetBookings() => _bookings.OrderByDescending(b => b.CreatedAt).ToList();

    public List<Booking> GetCustomerBookings(int customerId) => 
        _bookings.Where(b => b.CustomerId == customerId).OrderByDescending(b => b.CheckIn).ToList();

    public void CancelBooking(int bookingId)
    {
        var booking = _bookings.FirstOrDefault(b => b.Id == bookingId);
        if (booking != null)
        {
            booking.Status = BookingStatus.Cancelled;
            AddHistory(booking.RoomId, "Booking Cancelled", $"Booking #{bookingId} cancelled", booking.CustomerId);
        }
    }

    public List<RoomHistory> GetRoomHistory(int roomId) => 
        _roomHistory.Where(h => h.RoomId == roomId).OrderByDescending(h => h.Timestamp).ToList();

    private void AddHistory(int roomId, string eventType, string description, int? customerId = null)
    {
        _roomHistory.Add(new RoomHistory
        {
            Id = _nextHistoryId++,
            RoomId = roomId,
            Timestamp = DateTime.Now,
            EventType = eventType,
            Description = description,
            CustomerId = customerId
        });
    }

    private void UpdateLoyaltyTier(Customer customer)
    {
        customer.LoyaltyTier = customer.LoyaltyPoints switch
        {
            >= 2000 => "Platinum",
            >= 1000 => "Gold",
            >= 500 => "Silver",
            _ => "Bronze"
        };
    }

    public Dictionary<int, string> GetRoomStatus()
    {
        var today = DateTime.Today;
        var status = new Dictionary<int, string>();

        foreach (var room in _rooms)
        {
            // Compare dates (ignore time components) so bookings that start later today still count as "includes today".
            var currentBooking = _bookings.FirstOrDefault(b =>
                b.RoomId == room.Id &&
                b.Status != BookingStatus.Cancelled &&
                b.CheckIn.Date <= today &&
                b.CheckOut.Date > today);

            status[room.Id] = currentBooking != null ? "Occupied" : "Available";
        }

        return status;
    }
}
