namespace PRM_Backend_Server.ViewModels.Response
{
    public class UserManagementResponse
    {
        //ProfileDTO
        public class UserProfileResponse
        {
            public string name { get; set; }
            public string email { get; set; }
            public string phone { get; set; }
            public string address { get; set; }
        }
        public class WorkerProfileResponse
        {
            public string name { get; set; }
            public string email { get; set; }
            public string phone { get; set; }
            public string address { get; set; }
            public int experienceYears { get; set; }
            public string bio { get; set; }
            public bool isAvailable { get; set; }
            public int TotalReviews { get; set; }
            public decimal AverageRating { get; set; }
        }
        public class UpdateUserProfileResponse
        {
            public string message { get; set; }
        }

        //AdminManagementDTO
        public class DeleteUserResponse
        {
            public string message { get; set; }
        }
        public class UpdateUserResponse
        {
            public string message { get; set; }
        }
        public class BulkUpdateUserResponse
        {
            public string message { get; set; }
        }
        public class BulkDeleteUserResponse
        {
            public string message { get; set; }
        }


    }
}
