using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Managed.Adb;

namespace SW_Easy_Way.Modules
{
	public class Island
	{
		private readonly Device _device;
		private readonly MainWindow _mWindow;

		public Island(Device device, MainWindow mWindow)
		{
			_device = device;
			_mWindow = mWindow;
		}

		private static string GetPath(string img)
		{
			return $@"Resources/Island/{img}.bmp";
		}

		public Feedback GoToPlace(Activity location)
		{
			Functions.DoSwipe(new Rectangle(68, 452, 53, 50), new Rectangle(461, 452, 69, 42), _device);

			switch (location)
			{
				case Activity.WorldBoss:
					Functions.DoTap(_device, new Rectangle(694, 276, 46, 47));
					break;
				case Activity.Dragon:
				case Activity.Giant:
				case Activity.Necro:
				case Activity.ElementalHall:
				case Activity.MagicHall:
					Functions.DoTap(_device, new Rectangle(483, 428, 54, 54));
					break;
				case Activity.FriendGift:
					break;
				case Activity.ArenaRival:
					Functions.DoTap(_device, new Rectangle(80, 105, 44, 38));
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(location), location, null);
			}
			_mWindow.NewLog($"Going to: {location.GetDescription()}");
			Thread.Sleep(3000);
			return Feedback.Success;
		}

		public Feedback IslandToWord()
		{
			var rec = new Rectangle(510, 466, 57, 57);
			var template = (Bitmap)Image.FromFile(GetPath("battle_btn"));
			var source = (Bitmap)_device.Screenshot.ToImage();

			if (!Functions.CheckSimilarity(source, template, rec, 0.85)) return Feedback.Failure;

			Functions.DoTap(_device, rec);
			return Feedback.Success;
		}

		public Feedback ReturnToIsland()
		{
			var d = 0;
			var done = Feedback.Failure;

			var source = (Bitmap)_device.Screenshot.ToImage();
			var recB = new Rectangle(510, 466, 57, 57);
			var temB = (Bitmap)Image.FromFile(GetPath("battle_btn"));
			if (Functions.CheckSimilarity(source, temB, recB, 0.85)) return Feedback.Success;

			while (done == Feedback.Failure)
			{
				// Tap ESC
				Functions.DoAdbCommand("input keyevent 4", _device);
				Thread.Sleep(500);

				source = (Bitmap)_device.Screenshot.ToImage();
				var rec = new Rectangle(430, 196, 99, 23);
				var template = (Bitmap)Image.FromFile(GetPath("end_now"));
				if (!Functions.CheckSimilarity(source, template, rec, 0.85))
				{
					rec = new Rectangle(510, 466, 57, 57);
					template = (Bitmap)Image.FromFile(GetPath("battle_btn"));
					if (Functions.CheckSimilarity(source, template, rec, 0.85))
					{
						done = Feedback.Success;
					}
				}
				Thread.Sleep(1000);
				d++;
				if (d > 10) break;
			}
			return done;
		}
	}
}
