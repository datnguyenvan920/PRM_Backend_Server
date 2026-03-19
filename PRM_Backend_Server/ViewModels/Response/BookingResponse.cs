namespace PRM_Backend_Server.ViewModels.Response
{
    public class BookingResponse
    {
        public int BookingId { get; set; }
        public string? BookingCode { get; set; }
        public int CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public int? WorkerId { get; set; }
        public string? WorkerName { get; set; }
        public int PackageId { get; set; }
        public string? PackageName { get; set; }
        public DateOnly BookingDate { get; set; }
        public TimeOnly StartTime { get; set; }
        public TimeOnly? EndTime { get; set; }
        public string Address { get; set; } = string.Empty;
        public string? Note { get; set; }
        public decimal? TotalPrice { get; set; }
        public string? Status { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public PackageInfoResponse? Package { get; set; }
        public UserInfoResponse? Customer { get; set; }
        public UserInfoResponse? Worker { get; set; }
        public List<RatingInfoResponse>? Ratings { get; set; }
    }

    public class PackageInfoResponse
    {
        public int PackageId { get; set; }
        public string PackageName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public int DurationHours { get; set; }
        public CategoryInfoResponse? Category { get; set; }
    }

    public class CategoryInfoResponse
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
    }

    public class UserInfoResponse
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
    }

    public class RatingInfoResponse
    {
        public int RatingId { get; set; }
        public int RatingScore { get; set; }
        public string? Comment { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}
