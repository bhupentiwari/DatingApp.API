using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DatingApp.API.Helpers;
using DatingApp.API.Models;
using Microsoft.EntityFrameworkCore;

namespace DatingApp.API.Data
{
    public class DatingRepository : IDatingRepository
    {
        private readonly DataContext _context;
        public DatingRepository(DataContext context)
        {
            _context = context;
        }
        public void Add<T>(T entity) where T : class
        {
            _context.Add(entity);
        }

        public void Delete<T>(T entity) where T : class
        {
           _context.Remove(entity);
        }

        public async Task<User> GetUser(int id)
        {
          var user = await _context.Users.Include(p => p.Photos).FirstOrDefaultAsync(u => u.Id == id);
          return user;
        }

        public async Task<PagedList<User>> GetUsers(UserParams userParams)
        {
           var users =  _context.Users.Include(p => p.Photos).OrderByDescending(p =>p.LastActive).AsQueryable();     
           users = users.Where(p => p.Id != userParams.UserId);
           users = users.Where(p => p.Gender == userParams.Gender);
        //    if(userParams.MinAge==0)
        //    userParams.MinAge =18;
        //    if(userParams.MaxAge ==0)
        //    userParams.MaxAge = 99;
            if(userParams.MinAge!=18 || userParams.MaxAge!=99){
                var minDob = DateTime.Today.AddYears(-userParams.MaxAge-5);
                var maxDob = DateTime.Today.AddYears(-userParams.MinAge);

                users = users.Where(u => u.DateOfBirth >= minDob && u.DateOfBirth <= maxDob);
            }

            if(!string.IsNullOrEmpty(userParams.OrderBy)){
                switch(userParams.OrderBy){
                    case "created":
                     users = users.OrderByDescending(u => u.Created);
                     break;
                    
                    default:
                     users = users.OrderByDescending(u => u.LastActive);
                     break;
                }
            }
           return await PagedList<User>.CreateAsync(users,userParams.PageNumber,userParams.PageSize);
           
        }

        public async Task<bool> SaveAll()
        {
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<Photo> GetPhoto(int id){
            var photo = await _context.Photos.FirstOrDefaultAsync(c => c.Id == id);
            return photo;

        }

        public async Task<Photo> GetMainPhotoForUser(int id)
        {
            var photo = await _context.Photos.FirstOrDefaultAsync(p => p.Id ==id );
            return photo;
        }
    }
}