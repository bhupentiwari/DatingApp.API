using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DatingApp.API.Data;
using DatingApp.API.Dtos;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Options;
using DatingApp.API.Helpers;
using DatingApp.API.Models;

namespace DatingApp.API.Controllers
{
    [ApiController]
    [Route("api/users/{userid}/photos")]
    [Authorize]
    public class PhotosController : ControllerBase
    {
        public IDatingRepository _Repo { get; }
        private readonly IMapper _mapper;
        private readonly IOptions<CloudinarySettings> _cloudinarysetting;
        private Cloudinary _cloudinary;
        public PhotosController(IDatingRepository rep, IMapper mapper, IOptions<CloudinarySettings> cloudinarysetting)
        {
            this._cloudinarysetting = cloudinarysetting;
            this._mapper = mapper;
            this._Repo = rep;

            Account acc =new Account(
                _cloudinarysetting.Value.CloudName,
                _cloudinarysetting.Value.ApiKey,
                _cloudinarysetting.Value.ApiSecret
            );

            _cloudinary = new Cloudinary(acc);

        }

        [HttpGet("{id}", Name ="GetPhoto")]
        public async Task<IActionResult> GetPhoto(int id){

            var photoFromRepo = await _Repo.GetPhoto(id);
            var photo = _mapper.Map<PhotoForReturnDto>(photoFromRepo);
            return  Ok(photo);

        }
        [HttpPost]
        public async Task<IActionResult> AddPhotoForUser(int UserID,
                                [FromForm]PhotoforCreationDto  photoforCreationDto) {

             if (UserID != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value )) 
              {
                   return Unauthorized();
              }
            var userFromRep = await _Repo.GetUser(UserID);

            var file = photoforCreationDto.File;

            var uploadResult = new ImageUploadResult();

            if(file.Length>0){

                using( var str = file.OpenReadStream()){
                    var uploadParams =new ImageUploadParams(){
                        File = new FileDescription (file.Name,str),
                        Transformation = new Transformation()
                            .Width(500).Height(500).Crop("fill").Gravity("face")
            
                    };

                uploadResult = _cloudinary.Upload(uploadParams);
                }
            }
            photoforCreationDto.Url = uploadResult.Uri.ToString();
            photoforCreationDto.PublicId = uploadResult.PublicId.ToString();

            var photo = _mapper.Map<Photo>(photoforCreationDto);

            if(!userFromRep.Photos.Any(x =>x.IsMain)){
                photo.IsMain = true;
            }
            userFromRep.Photos.Add(photo);

            if(await _Repo.SaveAll()){

                var photToReturnDto = _mapper.Map<PhotoForReturnDto>(photo);
                return CreatedAtRoute("GetPhoto",new { id = photo.Id},photToReturnDto);                
            }
            return BadRequest("Could not add the photo");
        }
    }
}