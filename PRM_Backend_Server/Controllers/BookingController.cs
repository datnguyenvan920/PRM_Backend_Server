using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PRM_Backend_Server.Models;
using PRM_Backend_Server.ViewModels.Request;
using PRM_Backend_Server.ViewModels.Response;

namespace PRM_Backend_Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BookingController : ControllerBase
    {
        private static readonly HashSet<string> ValidStatuses = new(StringComparer.OrdinalIgnoreCase)
        {
            "pending", "confirmed", "in_progress", "completed", "cancelled"
        };

        private readonly HomeServiceAppContext _context;

        public BookingController(HomeServiceAppContext context)
        {
            _context = context;
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<BookingResponse>> GetBookingById(int id)
        {
            var booking = await _context.Bookings
                .Include(b => b.Customer)
                .Include(b => b.Worker)
                .Include(b => b.Package)
                    .ThenInclude(p => p.Category)
                .Include(b => b.Ratings)
                .FirstOrDefaultAsync(b => b.BookingId == id);

            if (booking == null)
            {
                return NotFound(new { message = "Booking not found" });
            }

            return Ok(MapBookingResponse(booking));
        }

        [HttpGet("customer/{customerId:int}")]
        public async Task<ActionResult<IEnumerable<BookingResponse>>> GetBookingsByCustomer(int customerId)
        {
            var bookings = await _context.Bookings
                .Include(b => b.Customer)
                .Include(b => b.Worker)
                .Include(b => b.Package)
                    .ThenInclude(p => p.Category)
                .Include(b => b.Ratings)
                .Where(b => b.CustomerId == customerId)
                .OrderByDescending(b => b.BookingDate)
                .ThenByDescending(b => b.StartTime)
                .ToListAsync();

            return Ok(bookings.Select(MapBookingResponse));
        }

        [HttpPost]
        public async Task<ActionResult<BookingResponse>> CreateBooking([FromBody] BookingRequest.CreateBookingRequest request)
        {
            if (request == null)
            {
                return BadRequest(new { message = "Invalid request" });
            }

            if (string.IsNullOrWhiteSpace(request.Address))
            {
                return BadRequest(new { message = "Address is required" });
            }

            if (request.BookingDate < DateOnly.FromDateTime(DateTime.Today))
            {
                return BadRequest(new { message = "Booking date cannot be in the past" });
            }

            var customer = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == request.CustomerId && u.Role == "customer");

            if (customer == null)
            {
                return BadRequest(new { message = "Customer does not exist" });
            }

            var package = await _context.ServicePackages
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.PackageId == request.PackageId && (p.IsActive ?? false));

            if (package == null)
            {
                return BadRequest(new { message = "Service package does not exist or is inactive" });
            }

            User? workerUser = null;
            if (request.WorkerId.HasValue)
            {
                workerUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.UserId == request.WorkerId.Value && u.Role == "worker");

                var workerInfo = await _context.Workers
                    .FirstOrDefaultAsync(w => w.WorkerId == request.WorkerId.Value && (w.IsAvailable ?? false));

                if (workerUser == null || workerInfo == null)
                {
                    return BadRequest(new { message = "Worker does not exist or is unavailable" });
                }
            }

            var booking = new Booking
            {
                BookingCode = GenerateBookingCode(),
                CustomerId = request.CustomerId,
                WorkerId = request.WorkerId,
                PackageId = request.PackageId,
                BookingDate = request.BookingDate,
                StartTime = request.StartTime,
                EndTime = request.StartTime.AddHours(package.DurationHours),
                Address = request.Address.Trim(),
                Note = request.Note,
                TotalPrice = package.Price,
                Status = request.WorkerId.HasValue ? "confirmed" : "pending",
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();

            booking.Customer = customer;
            booking.Worker = workerUser;
            booking.Package = package;
            booking.Ratings = new List<Rating>();

            return CreatedAtAction(nameof(GetBookingById), new { id = booking.BookingId }, MapBookingResponse(booking));
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<BookingResponse>> UpdateBooking(int id, [FromBody] BookingRequest.UpdateBookingRequest request)
        {
            if (request == null)
            {
                return BadRequest(new { message = "Invalid request" });
            }

            var booking = await _context.Bookings
                .Include(b => b.Customer)
                .Include(b => b.Worker)
                .Include(b => b.Package)
                    .ThenInclude(p => p.Category)
                .Include(b => b.Ratings)
                .FirstOrDefaultAsync(b => b.BookingId == id);

            if (booking == null)
            {
                return NotFound(new { message = "Booking not found" });
            }

            if (request.WorkerId.HasValue)
            {
                var workerUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.UserId == request.WorkerId.Value && u.Role == "worker");

                var workerInfo = await _context.Workers
                    .FirstOrDefaultAsync(w => w.WorkerId == request.WorkerId.Value && (w.IsAvailable ?? false));

                if (workerUser == null || workerInfo == null)
                {
                    return BadRequest(new { message = "Worker does not exist or is unavailable" });
                }

                booking.WorkerId = request.WorkerId.Value;
                booking.Worker = workerUser;
            }

            if (request.BookingDate.HasValue)
            {
                if (request.BookingDate.Value < DateOnly.FromDateTime(DateTime.Today))
                {
                    return BadRequest(new { message = "Booking date cannot be in the past" });
                }

                booking.BookingDate = request.BookingDate.Value;
            }

            if (request.StartTime.HasValue)
            {
                booking.StartTime = request.StartTime.Value;
                booking.EndTime = request.StartTime.Value.AddHours(booking.Package.DurationHours);
            }

            if (!string.IsNullOrWhiteSpace(request.Address))
            {
                booking.Address = request.Address.Trim();
            }

            if (request.Note != null)
            {
                booking.Note = request.Note;
            }

            if (!string.IsNullOrWhiteSpace(request.Status))
            {
                if (!ValidStatuses.Contains(request.Status))
                {
                    return BadRequest(new { message = "Invalid booking status" });
                }

                booking.Status = request.Status.ToLowerInvariant();
            }

            booking.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            return Ok(MapBookingResponse(booking));
        }

        [HttpDelete("{id:int}")]
        public async Task<ActionResult<BookingResponse>> DeleteBooking(int id)
        {
            var booking = await _context.Bookings
                .Include(b => b.Customer)
                .Include(b => b.Worker)
                .Include(b => b.Package)
                    .ThenInclude(p => p.Category)
                .Include(b => b.Ratings)
                .FirstOrDefaultAsync(b => b.BookingId == id);

            if (booking == null)
            {
                return NotFound(new { message = "Booking not found" });
            }

            if (booking.Status?.Equals("completed", StringComparison.OrdinalIgnoreCase) == true)
            {
                return BadRequest(new { message = "Completed booking cannot be cancelled" });
            }

            booking.Status = "cancelled";
            booking.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return Ok(MapBookingResponse(booking));
        }

        private static string GenerateBookingCode()
            => $"BK{DateTime.Now:yyyyMMddHHmmssfff}";

        private static BookingResponse MapBookingResponse(Booking booking)
        {
            return new BookingResponse
            {
                BookingId = booking.BookingId,
                BookingCode = booking.BookingCode,
                CustomerId = booking.CustomerId,
                CustomerName = booking.Customer?.FullName,
                WorkerId = booking.WorkerId,
                WorkerName = booking.Worker?.FullName,
                PackageId = booking.PackageId,
                PackageName = booking.Package?.PackageName,
                BookingDate = booking.BookingDate,
                StartTime = booking.StartTime,
                EndTime = booking.EndTime,
                Address = booking.Address,
                Note = booking.Note,
                TotalPrice = booking.TotalPrice,
                Status = booking.Status,
                CreatedAt = booking.CreatedAt,
                UpdatedAt = booking.UpdatedAt,
                Package = booking.Package == null ? null : new PackageInfoResponse
                {
                    PackageId = booking.Package.PackageId,
                    PackageName = booking.Package.PackageName,
                    Description = booking.Package.Description,
                    Price = booking.Package.Price,
                    DurationHours = booking.Package.DurationHours,
                    Category = booking.Package.Category == null ? null : new CategoryInfoResponse
                    {
                        CategoryId = booking.Package.Category.CategoryId,
                        CategoryName = booking.Package.Category.CategoryName
                    }
                },
                Customer = booking.Customer == null ? null : new UserInfoResponse
                {
                    UserId = booking.Customer.UserId,
                    FullName = booking.Customer.FullName,
                    Email = booking.Customer.Email,
                    Phone = booking.Customer.Phone
                },
                Worker = booking.Worker == null ? null : new UserInfoResponse
                {
                    UserId = booking.Worker.UserId,
                    FullName = booking.Worker.FullName,
                    Email = booking.Worker.Email,
                    Phone = booking.Worker.Phone
                },
                Ratings = booking.Ratings?.Select(r => new RatingInfoResponse
                {
                    RatingId = r.RatingId,
                    RatingScore = r.RatingScore ?? 0,
                    Comment = r.Comment,
                    CreatedAt = r.CreatedAt
                }).OrderByDescending(r => r.CreatedAt).ToList()
            };
        }
    }
}
