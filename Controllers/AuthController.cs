using System;
using System.Collections.Generic;

using System.Linq;

using System.Text;
using System.Threading.Tasks;
using DatingApp.API.Data;
using DatingApp.API.Dtos;
using DatingApp.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace DatingApp.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]

    public class AuthController : ControllerBase
    {
        private readonly IAuthRepository _repo;

        public IConfiguration _config { get; }

        public AuthController(IAuthRepository repo,IConfiguration config)
        {
            _repo = repo;
            _config = config;
        }

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register(UserForRegisterDto userForRegisterDto ){
                
                userForRegisterDto.username = userForRegisterDto.username.ToLower();

                if(await _repo.IsExists(userForRegisterDto.username))
                return BadRequest("Username already exist");
                var userToCreate =new User
                {
                    Username = userForRegisterDto.username
                };
                var createUser = await _repo.Register(userToCreate,userForRegisterDto.password);
                return StatusCode(201);

        }
        [HttpPost("login")]
        
        public async Task<IActionResult> Login(UserForLoginDto userForLoginDto){

            var userFromRep = await _repo.Login(userForLoginDto.Username,userForLoginDto.Password);
            
            if(userFromRep == null)
            return Unauthorized();

            var claimes = new []{
                new Claim(ClaimTypes.NameIdentifier,userFromRep.Id.ToString()),
                new Claim(ClaimTypes.Name,userFromRep.Username)
            };
            
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                        _config.GetSection("AppSetting:Token").Value));

            var cred = new SigningCredentials(key,SecurityAlgorithms.HmacSha512Signature);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claimes),
                Expires = DateTime.Now.AddDays(1),
                SigningCredentials =  cred
            };

            var tokenHandler = new JwtSecurityTokenHandler();

            var token = tokenHandler.CreateToken(tokenDescriptor);

            return Ok(new {
                token = tokenHandler .WriteToken(token)
            });
        }        
    }
}