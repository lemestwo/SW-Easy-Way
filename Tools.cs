using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SW_Easy_Way.Interceptor.Infos;

namespace SW_Easy_Way
{
	public static class Tools
	{
		public static void ConvertMonstersFromNodeProxy(string path, string outPath)
		{
			var content = JsonConvert.DeserializeObject<Dictionary<int, string>>(File.ReadAllText(path));

			var myList = content.Select(i => new SwMonsters { Id = i.Key, Name = i.Value }).ToList();

			using (var sw = File.CreateText(outPath))
			{
				var serializer = new JsonSerializer();
				serializer.Serialize(sw, myList);
			}
			/*var serializer = new JsonSerializer();
			using (var sw = new StreamWriter(@"D:/b.json"))
			{
				using (var jtw = new JsonTextWriter(sw))
				{
					serializer.Serialize(jtw, myList);
				}
			}*/
		}
	}
}
