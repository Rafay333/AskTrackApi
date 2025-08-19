using AskTrackApi.Data;
using Microsoft.AspNetCore.Mvc;
using AskTrackApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Cors;

namespace AskTrackApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [EnableCors("AllowAll")]
    public class AuthController : ControllerBase
    {
        private readonly RemkDataContext _context;
        private readonly JwtService _jwt;

        public AuthController(RemkDataContext context, JwtService jwt)
        {
            _context = context;
            _jwt = jwt;
        }

        [HttpGet("test")]
        public IActionResult Test()
        {
            return Ok(new { message = "CORS is working!", timestamp = DateTime.UtcNow });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var installer = await _context.Installers
                .FirstOrDefaultAsync(i => i.Int_number == request.Int_number &&
                                          i.Int_code == request.Int_code &&
                                          i.Int_pass == request.Int_pass);

            if (installer == null)
            {
                return Unauthorized(new { message = "Invalid credentials" });
            }

            var token = _jwt.GenerateToken(installer);

            return Ok(new
            {
                message = "Login successful",
                token = token,
                installer.Int_number,
                installer.Int_code,
                installer.Int_type,
                installer.Int_Branch
            });
        }

    }
}
// Models/LoginRequest.cs