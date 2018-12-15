using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DatingApp.API.Data;
using DatingApp.API.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;

namespace DatingApp.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]    
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly IDatingRepository _repo;
        private readonly IMapper _mapper;

        public UsersController(IDatingRepository repo,IMapper mapper)
        {
            _repo = repo;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<IActionResult> GetUsers() 
        {
            var users  = await _repo.GetUsers();
            var userToReturn = _mapper.Map<IEnumerable<UserForDetailedDto>>(users);
            return Ok(userToReturn);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetUser(int id) 
        {
          
            var user = await _repo.GetUser(id);
            var userToReturn = _mapper.Map<UserForDetailedDto>(user);
            return Ok(userToReturn);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody]UserForUpdateDto userForUpdateDto){

            if (id != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value )) {
                   return Unauthorized();
               }
            var userFromRep = await _repo.GetUser(id);
            
            _mapper.Map(userForUpdateDto, userFromRep);

            if(await _repo.SaveAll())
                return NoContent();

            throw new Exception($"Updating user {id} faild on Save");

        }

    }
}