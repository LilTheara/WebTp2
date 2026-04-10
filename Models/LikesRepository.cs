using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using DAL;
using Models;

namespace Models
{
	public class LikesRepository : Repository<Like>
	{
        public static List<User> GetMediaLikes(int mediaId)
        {
            return DB.Likes.ToList()
                .Where(l => l.MediaId == mediaId)
                .Select(l => DB.Users.Get(l.OwnerId))
                .Where(u => u != null)
                .ToList();
        }
        public void DeleteByMediaId(int mediaId)
        {
            var likes = ToList().Where(l => l.MediaId == mediaId).ToList();
            foreach (var like in likes)
            {
                Delete(like.Id);
            }
        }

        public void DeleteByUserId(int userId)
        {
            var likes = ToList().Where(l => l.OwnerId == userId).ToList();
            foreach (var like in likes)
            {
                Delete(like.Id);
            }
        }

        public int CountByMediaId(int mediaId)
        {
            return ToList().Count(l => l.MediaId == mediaId);
        }
       
    }
}