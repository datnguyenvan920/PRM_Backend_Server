using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PRM_Backend_Server.Models;
using PRM_Backend_Server.ViewModels.Request;
using PRM_Backend_Server.ViewModels.Response;

namespace PRM_Backend_Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RatingController : ControllerBase
    {
        private readonly HomeServiceAppContext _context;

        public RatingController(HomeServiceAppContext context)
        {
            _context = context;
        }

        [HttpGet("booking/{bookingId:int}")]
        public async Task<ActionResult<RatingResponse>> GetRatingByBooking(int bookingId)
        {
            var rating = await _context.Ratings
                .Include(r => r.Booking)
                .Include(r => r.Customer)
                .Include(r => r.Worker)
                .Where(r => r.BookingId == bookingId)
                .OrderByDescending(r => r.CreatedAt)
                .FirstOrDefaultAsync();

            if (rating == null)
            {
                return NotFound(new { message = "Rating not found" });
            }

            return Ok(MapRatingResponse(rating));
        }

        [HttpGet("customer/{customerId:int}")]
        public async Task<ActionResult<IEnumerable<RatingResponse>>> GetRatingsByCustomer(int customerId)
        {
            var ratings = await _context.Ratings
                .Include(r => r.Booking)
                .Include(r => r.Customer)
                .Include(r => r.Worker)
                .Where(r => r.CustomerId == customerId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return Ok(ratings.Select(MapRatingResponse));
        }

        [HttpPost]
        public async Task<ActionResult<RatingResponse>> CreateRating([FromBody] RatingRequest.CreateRatingRequest request)
        {
            if (request == null)
            {
                return BadRequest(new { message = "Invalid request" });
            }

            if (request.RatingScore < 1 || request.RatingScore > 5)
            {
                return BadRequest(new { message = "Rating score must be between 1 and 5" });
            }

            var booking = await _context.Bookings
                .Include(b => b.Customer)
                .Include(b => b.Worker)
                .FirstOrDefaultAsync(b => b.BookingId == request.BookingId);

            if (booking == null)
            {
                return BadRequest(new { message = "Booking does not exist" });
            }

            if (booking.CustomerId != request.CustomerId)
            {
                return BadRequest(new { message = "Customer does not match booking" });
            }

            if (!string.Equals(booking.Status, "completed", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { message = "Only completed bookings can be rated" });
            }

            if (!booking.WorkerId.HasValue)
            {
                return BadRequest(new { message = "Booking does not have worker to rate" });
            }

            var existed = await _context.Ratings
                .FirstOrDefaultAsync(r => r.BookingId == request.BookingId && r.CustomerId == request.CustomerId);

            if (existed != null)
            {
                return BadRequest(new { message = "This booking has already been rated by this customer" });
            }

            var rating = new Rating
            {
                BookingId = request.BookingId,
                CustomerId = request.CustomerId,
                WorkerId = booking.WorkerId.Value,
                RatingScore = request.RatingScore,
                Comment = request.Comment,
                CreatedAt = DateTime.Now
            };

            _context.Ratings.Add(rating);
            await _context.SaveChangesAsync();

            rating.Booking = booking;
            rating.Customer = booking.Customer;
            rating.Worker = booking.Worker!;

            return Ok(MapRatingResponse(rating));
        }

        [HttpPut("{ratingId:int}")]
        public async Task<ActionResult<RatingResponse>> UpdateRating(int ratingId, [FromBody] RatingRequest.UpdateRatingRequest request)
        {
            if (request == null)
            {
                return BadRequest(new { message = "Invalid request" });
            }

            var rating = await _context.Ratings
                .Include(r => r.Booking)
                .Include(r => r.Customer)
                .Include(r => r.Worker)
                .FirstOrDefaultAsync(r => r.RatingId == ratingId);

            if (rating == null)
            {
                return NotFound(new { message = "Rating not found" });
            }

            if (request.RatingScore.HasValue)
            {
                if (request.RatingScore.Value < 1 || request.RatingScore.Value > 5)
                {
                    return BadRequest(new { message = "Rating score must be between 1 and 5" });
                }

                rating.RatingScore = request.RatingScore.Value;
            }

            if (request.Comment != null)
            {
                rating.Comment = request.Comment;
            }

            await _context.SaveChangesAsync();

            return Ok(MapRatingResponse(rating));
        }

        [HttpDelete("{ratingId:int}")]
        public async Task<IActionResult> DeleteRating(int ratingId)
        {
            var rating = await _context.Ratings.FirstOrDefaultAsync(r => r.RatingId == ratingId);
            if (rating == null)
            {
                return NotFound(new { message = "Rating not found" });
            }

            _context.Ratings.Remove(rating);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Rating deleted successfully" });
        }

        private static RatingResponse MapRatingResponse(Rating rating)
        {
            return new RatingResponse
            {
                RatingId = rating.RatingId,
                BookingId = rating.BookingId,
                BookingCode = rating.Booking?.BookingCode,
                CustomerId = rating.CustomerId,
                CustomerName = rating.Customer?.FullName,
                WorkerId = rating.WorkerId,
                WorkerName = rating.Worker?.FullName,
                RatingScore = rating.RatingScore,
                Comment = rating.Comment,
                CreatedAt = rating.CreatedAt
            };
        }
    }
}
