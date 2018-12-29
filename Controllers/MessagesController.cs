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
using DatingApp.API.Helpers;
using DatingApp.API.Models;

namespace DatingApp.API.Controllers
{
    [ServiceFilter(typeof(LogUserActivity))]
    [ApiController]
    [Route("api/users/{userId}/[controller]")]
    [Authorize]
    public class MessagesController : ControllerBase
    {
        private readonly IDatingRepository _repo;
        private readonly IMapper _mapper;
        public MessagesController(IDatingRepository repo, IMapper mapper)
        {
            _repo = repo;
            _mapper = mapper;
        }

        [HttpGet("{Id}" , Name="GetMessage")]
        public async Task<IActionResult> GetMessage(int userId,int Id){

            if (userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value )) {
                   return Unauthorized();
               }

            var messageFromRepo = await _repo.GetMessage(Id);

            if(messageFromRepo==null){
                return NotFound();
            }

            return Ok(messageFromRepo);
        }

        [HttpGet]
        public async Task<IActionResult>  GetMessagesForUser(int userId, [FromQuery]MessageParams messageParams)
        {
            if (userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value )) {
                   return Unauthorized();
               }

            messageParams.UserId = userId;
            
            var messageFromRepo = await _repo.GetMessagesForUser(messageParams);

            var message = _mapper.Map<IEnumerable<MessageToReturnDto>>(messageFromRepo);

            Response.AddPagination(messageFromRepo.CurrentPage,messageFromRepo.PageSize,
            messageFromRepo.TotalCount,messageFromRepo.TotalPages);

            return Ok(message);

        }
        [HttpGet("thread/{recipientId}")]
        public async Task<IActionResult> GetMessageThread(int userId, int recipientId) {
            if (userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value )) {
                   return Unauthorized();
                }
            var messageFromRepo =await  _repo.GetMessageThread(userId,recipientId);
            var messageThread = _mapper.Map<IEnumerable<MessageToReturnDto>>(messageFromRepo);

            return Ok(messageThread);
        }
        
        [HttpPost]
        public async Task<IActionResult> CreateMessage(int userId,MessageForCreationDto  messageForCreationDto)
        {
            var sender = await _repo.GetUser(userId);

            if (sender.Id != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value )) {
                   return Unauthorized();
               }

            messageForCreationDto.SenderId = userId;

            var recipient = _repo.GetUser(messageForCreationDto.RecipientId);

            if(recipient==null) {
                return BadRequest("Could not find user");
            }

            var message =  _mapper.Map<Message>(messageForCreationDto);

            _repo.Add(message);

            

            if(await _repo.SaveAll()){

                var messageToReturn = _mapper.Map<MessageToReturnDto>(message);
                return CreatedAtRoute("GetMessage", new {id = message.Id},messageToReturn);
            }

            throw new Exception("Unable to save the message");

        }

        [HttpPost("{id}")]
        public async Task<IActionResult> DeleteMessage(int id, int userId) {         
            if (userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value )) {
                   return Unauthorized();
            }
            var msgFromRepo = await _repo.GetMessage(id);

            if(msgFromRepo.SenderId == userId)
                msgFromRepo.SenderDeleted = true;            
            if(msgFromRepo.RecipientId == userId)
                msgFromRepo.RecipientDeleted = true;

            if (msgFromRepo.SenderDeleted &&  msgFromRepo.RecipientDeleted)
            {
                _repo.Delete(msgFromRepo);
            }
            if(await _repo.SaveAll())
                return NoContent();

            throw new Exception("Error deleting the message");
        }

    [HttpPost("{id}/read")]
    public async Task<IActionResult> MaskMessageAsRead(int userId,int id){

        if (userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value )) 
            return Unauthorized();
           
        var msgFromRepo = await _repo.GetMessage(id);

        if(msgFromRepo.RecipientId != userId)
            return Unauthorized();
        msgFromRepo.IsRead = true;
        msgFromRepo.DateRead = DateTime.Now;
        
        await _repo.SaveAll();
            return NoContent();
    }
    }
}