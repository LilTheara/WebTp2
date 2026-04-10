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
			return DB.Likes.ToList().Where(l => l.MediaId == mediaId)	//va chercher les likes du media spécifié
				.Select(l => DB.Users.Get(l.OwnerId)).ToList();	//va chercher les usagers qui ont aimer ce média
		}
	}
}