using HBS.Models;

namespace HBS.Services;

public interface IHotelService
{
    List<Room> GetAvailableRooms(DateTime checkIn, DateTime checkOut, RoomCategory? category = null);
    Room? GetRoom(int roomId);
    List<Room> GetAllRooms();
    Booking CreateBooking(int roomId, int customerId, DateTime checkIn, DateTime checkOut, int pointsToRedeem = 0);
    int CreateCustomer(string name, string email, string phone);
    Customer? GetCustomer(int id);
    Customer? GetCustomerByEmail(string email);
    List<Booking> GetBookings();
    List<Booking> GetCustomerBookings(int customerId);
    void CancelBooking(int bookingId);
    List<RoomHistory> GetRoomHistory(int roomId);
    Dictionary<int, string> GetRoomStatus();
}
