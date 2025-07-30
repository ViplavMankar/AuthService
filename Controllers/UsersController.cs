using AuthService.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    public UsersController(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<UserReadDto>> GetUserById(string id)
    {
        var user = await _dbContext.Users
                .Where(u => u.Id == id)
                .Select(u => new UserReadDto
                {
                    Id = u.Id,
                    Username = u.UserName
                })
                .FirstOrDefaultAsync();

        if (user == null)
        {
            return NotFound("User not found.");
        }
        return Ok(user);
    }
}