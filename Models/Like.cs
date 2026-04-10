using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using DAL;
using Models;
using Newtonsoft.Json;

namespace Models
{
	public class Like : Record
	{
		public int OwnerId { get; set; } = 1;
		[JsonIgnore]
		public User Owner => DB.Users.Get(OwnerId).Copy();
		public int MediaId { get; set; } = 1;
		[JsonIgnore]
		public Media Media => DB.Medias.Get(MediaId).Copy();

	}
}