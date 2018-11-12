using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Managed.Adb;
using SW_Easy_Way.Config;

namespace SW_Easy_Way.Modules
{
	public class Worldboss
	{
		private readonly Device _device;
		private readonly Routine _routine;
		private readonly Wb _config;
		private readonly Smartrune _smartune;

		private int _alreadyDid;

		public Worldboss(Device device, Routine routine, Wb config, Smartrune smartune)
		{
			_device = device;
			_routine = routine;
			_config = config;
			_smartune = smartune;
			_alreadyDid = 0;
		}

		private static string GetPath(string img)
		{
			return $@"Resources/Worldboss/{img}.bmp";
		}

		public Feedback CheckWorldBoss()
		{
			// TODO: CHECK IF COOLDOWN
			var rec = new Rectangle(440, 26, 120, 25);
			var template = (Bitmap)Image.FromFile(GetPath("worldboss"));
			var source = (Bitmap)_device.Screenshot.ToImage();
			if (!Functions.CheckSimilarity(source, template, rec, 0.85)) return Feedback.Failure;

			rec = new Rectangle(790, 324, 27, 31);
			source = (Bitmap)_device.Screenshot.ToImage();
			var check3Ent = Functions.CheckSimilarity(source, (Bitmap)Image.FromFile(GetPath("entrance_3")), rec, 0.92);
			if (check3Ent) _alreadyDid = 0;
			else
			{
				var check2Ent = Functions.CheckSimilarity(source, (Bitmap)Image.FromFile(GetPath("entrance_2")), rec, 0.92);
				if (check2Ent) _alreadyDid = 1;
				else
				{
					var check1Ent = Functions.CheckSimilarity(source, (Bitmap)Image.FromFile(GetPath("entrance_1")), rec, 0.92);
					_alreadyDid = check1Ent ? 2 : 3;
				}
			}
			if (_alreadyDid >= _config.Repeat) return Feedback.EndThatRoutine;

			Functions.DoTap(_device, new Rectangle(698, 373, 87, 27));
			// TODO: OPTIONS IF WANNA REFRESH ENERGY OR GET FROM BOX
			// TODO: RIGHT NOW, ONLY SEND A FAILURE FEEDBACK
			return Functions.CheckNoEnergyMessage(_device) ? Feedback.EndThatRoutine : Feedback.Success;
		}

		public Feedback DoMonsterPreparation()
		{
			Functions.DoTap(_device, new Rectangle(780, 79, 91, 17));
			var rec = new Rectangle(459,311,42,29);
			var template = (Bitmap)Image.FromFile(GetPath("ok_btn"));
			var source = (Bitmap)_device.Screenshot.ToImage();
			if (Functions.CheckSimilarity(source, template, rec, 0.9))
			{
				return Feedback.NotEnoughMonsters;
			}
			Functions.DoTap(_device, new Rectangle(815, 356, 88, 41));
			source = (Bitmap)_device.Screenshot.ToImage();
			return Functions.CheckSimilarity(source, template, rec, 0.9) ? Feedback.NotEnoughMonsters : Feedback.Success;
		}

		public Feedback WaitForFinish()
		{
			for (var i = 0; i < 9; i++)
			{
				Functions.DoTap(_device, new Rectangle(880, 518, 32, 15));
			}
			var d = 0;
			while (true)
			{
				var rec = new Rectangle(447, 66, 35, 43);
				// TODO: CHANGE VERIFICATION IMAGE
				var template = (Bitmap)Image.FromFile(GetPath("result"));
				var source = (Bitmap)_device.Screenshot.ToImage();
				if (Functions.CheckSimilarity(source, template, rec, 0.75))
				{
					Functions.DoTap(_device, new Rectangle(732, 77, 74, 41), 2000);
					return Feedback.Success;
				}
				Thread.Sleep(5000);
				d++;
				if (d > 70) break;
			}
			return Feedback.Failure;
		}

		public Feedback HandleResults()
		{
			// TODO: HANDLE REWARDS
			Functions.DoTap(_device, new Rectangle(446, 482, 74, 26));
			Functions.DoTap(_device, new Rectangle(796, 115, 106, 56));
			Thread.Sleep(2000);
			return Feedback.Repeat;
		}
	}
}
