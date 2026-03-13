using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PRM_Backend_Server.Models;
using PRM_Backend_Server.ViewModels.Pagination;

namespace PRM_Backend_Server.Controllers
{
    [Route("user/[controller]")]
    [ApiController]
    public class UserManagementController : ControllerBase
    {
        [Authorize(Roles = "admin")]
        [HttpGet]
        public IActionResult GetUsers([FromQuery] PaginationRequest request)
        {
            
            return Ok(new PaginationResponse<User> {  });
        }
    }
}
