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
      
            if(userParams.Likers){
                var userLikers  = await GetUserLikes(userParams.UserId,userParams.Likers);
                users = users.Where(u => userLikers.Contains(u.Id));
            }

            if(userParams.Likees){
                var userLikees  = await GetUserLikes(userParams.UserId,userParams.Likers);
                users = users.Where(u => userLikees.Contains(u.Id));
            }
           
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

        public async Task<Like> GetLike(int userId, int recepientId)
        {
            /* If records doesn not exist it will return null */
           return await _context.Likes.FirstOrDefaultAsync(
               u => u.LikerId ==userId && u.LikeeId ==recepientId);
        }

        public async Task<IEnumerable<int>> GetUserLikes(int id,bool likers) {

            var user = await _context.Users
                    .Include(x =>x.Likers)
                    .Include(x => x.Likees)
                    .FirstOrDefaultAsync(u => u.Id == id);

            if(likers){
                return  user.Likers.Where(u => u.LikeeId ==id).Select(i => i.LikerId);
            }
            else{
                return  user.Likees.Where(u => u.LikerId ==id).Select(i => i.LikeeId);
            }
        }

        public async Task<Message> GetMessage(int Id)
        {
           return await _context.Messages.FirstOrDefaultAsync(m => m.Id == Id);
        }

        public async Task<PagedList<Message>> GetMessagesForUser(MessageParams messageParams)
        {
                var messages = _context.Messages
                     .Include(u =>u.Sender).ThenInclude(p =>p.Photos)
                     .Include(u =>u.Recipient).ThenInclude(p =>p.Photos)
                     .AsQueryable();

                switch(messageParams.MessageContainer){

                    case "Inbox":
                        messages = messages.Where(u => u.RecipientId == messageParams.UserId && u.RecipientDeleted ==false);
                        break;
                    case "Outbox":
                        messages = messages.Where(u => u.SenderId == messageParams.UserId && u.SenderDeleted ==false);
                        break;
                    default:
                        //Unread message
                        messages = messages.Where(u => u.RecipientId == messageParams.UserId && u.IsRead==false  && u.RecipientDeleted ==false);
                        break;

                }

            messages = messages.OrderByDescending(d => d.MessageSent);

            return await PagedList<Message>.CreateAsync(messages,
                         messageParams.PageNumber,messageParams.PageSize);
        }

        public async Task<IEnumerable<Message>> GetMessageThread(int userId, int recepientId)
        {
            var message = await _context.Messages
              .Include(u =>u.Sender).ThenInclude(p =>p.Photos)
              .Include(u =>u.Recipient).ThenInclude(p =>p.Photos)
              .Where(m => m.RecipientId == userId && m.RecipientDeleted ==false && m.SenderId == recepientId 
                    || m.RecipientId == recepientId && m.SenderId ==userId && m.SenderDeleted ==false)
              .OrderByDescending(m => m.MessageSent)
              .ToListAsync();
            return message;
        }
    }
}
