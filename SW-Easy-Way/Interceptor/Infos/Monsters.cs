using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SW_Easy_Way.Interceptor.Infos
{
	public class SwMonsters
	{
		public int Id { get; set; }
		public string Name { get; set; }

		public static List<SwMonsters> SwMonstersList { get; set; } = new List<SwMonsters>();
	}
}
