using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using Managed.Adb;
using Newtonsoft.Json.Linq;
using SW_Easy_Way.Config;
using SW_Easy_Way.Interceptor;
using SW_Easy_Way.Interceptor.Infos;
using static System.Int32;

namespace SW_Easy_Way.Modules
{
	public class Cairos
	{
		private readonly Device _device;
		private readonly Smartrune _smartrune;
		private readonly MainWindow _mWindow;
		private readonly Routine _routine;
		public Activity ActualActivity { get; set; }
		public int ActualFloor { get; set; }
		public RoutinePattern ActualPattern { get; set; }
		public int ActualTimes { get; set; }

		public Cairos(Device device, Smartrune smartrune, MainWindow mWindow, Routine routine)
		{
			_device = device;
			_smartrune = smartrune;
			_mWindow = mWindow;
			_routine = routine;
			CheckPause();
		}

		private static Bitmap GetImg(string img)
		{
			return (Bitmap)Image.FromFile($@"Resources/Cairos/{img}.bmp");
		}

		public Feedback SelectCairosDungeon()
		{
			var rec = new Rectangle(398, 38, 65, 21);
			var template = GetImg("check_cairos");
			var source = (Bitmap)_device.Screenshot.ToImage();

			if (!Functions.CheckSimilarity(source, template, rec, 0.85)) return Feedback.Failure;

			var img = "";
			var moreAct = false;

			switch (ActualActivity)
			{
				case Activity.Giant:
					img = "giant_btn";
					break;
				case Activity.Dragon:
					img = "dragon_btn";
					break;
				case Activity.Necro:
					img = "necro_btn";
					break;
				case Activity.MagicHall:
					img = "magic_btn";
					moreAct = true;
					break;
				// TODO: ELEMENTAL HALL
				case Activity.ElementalHall:
					img = "";
					moreAct = true;
					break;
			}
			if (moreAct)
			{
				Functions.DoSwipe(new Rectangle(197, 440, 123, 34), new Rectangle(212, 173, 108, 20), _device);
			}

			rec = new Rectangle(138, 157, 238, 351);
			template = GetImg(img);
			var check = Functions.CheckSimilarityDeep(source, rec, template, 0.70f);
			if (check == null) return Feedback.Failure;

			rec = new Rectangle(check[0].Rectangle.X + rec.X, check[0].Rectangle.Y + rec.Y, check[0].Rectangle.Width, check[0].Rectangle.Height);
			Functions.DoTap(_device, rec);

			if (!Functions.CheckNoEnergyMessage(_device)) return Feedback.Success;
			if (ActualPattern != RoutinePattern.RefreshAndRepeat) return Feedback.Failure;

			if (!Functions.BuyEnergyOrWingsFromMenu(_device, _mWindow.LogWizard.WizardInfo.SocialPointCurrent)) return Feedback.Failure;

			_mWindow.NewLog("Refreshed energy with success", LogType.Green);
			return Feedback.Success;
		}

		public Feedback SelectCairosFloor()
		{
			if (ActualFloor <= 4)
			{
				Functions.DoSwipe(new Rectangle(604, 204, 13, 15), new Rectangle(607, 454, 24, 19), _device);
				Functions.DoSwipe(new Rectangle(604, 204, 13, 15), new Rectangle(607, 454, 24, 19), _device);
			}
			else
			{
				Functions.DoSwipe(new Rectangle(607, 454, 24, 19), new Rectangle(604, 204, 13, 15), _device);
				Functions.DoSwipe(new Rectangle(607, 454, 24, 19), new Rectangle(604, 204, 13, 15), _device);
				if (ActualFloor < 7)
				{
					Functions.DoSwipe(new Rectangle(593, 167, 44, 3), new Rectangle(606, 490, 21, 6), _device);
				}
			}

			var rec = new Rectangle(500, 157, 61, 344);
			var source = (Bitmap)_device.Screenshot.ToImage();
			var template = GetImg($"floor_{ActualFloor}");
			var check = Functions.CheckSimilarityDeep(source, rec, template, 0.80f);
			if (check == null) return Feedback.Failure;

			Functions.DoTap(_device, new Rectangle(759, rec.Y + check[0].Rectangle.Y + 5, 28, 28));
			return Feedback.Success;
		}

		public Feedback DoMonstersPreparations()
		{
			const CommandPacket cmd = CommandPacket.BattleDungeonStart;

			var rec = new Rectangle(96, 93, 297, 205);
			var source = (Bitmap)_device.Screenshot.ToImage();
			var template = GetImg("missing_monster");
			if (Functions.CheckSimilarityDeep(source, rec, template, 0.90f) != null) return Feedback.Failure;

			// Battle Start Button
			Functions.ProxyWaitingAdd(_mWindow.LogWizard.Name, cmd, out var infoHeader);
			Functions.DoTap(_device, new Rectangle(771, 356, 80, 45));

			// Check no errors
			rec = new Rectangle(476, 195, 69, 24);
			template = GetImg("one_monster");
			source = (Bitmap)_device.Screenshot.ToImage();
			if (Functions.CheckSimilarity(source, template, rec, 0.85)) return Feedback.Failure;

			if (Functions.ProxyGetResponse(infoHeader, 1) != null)
			{
				return Feedback.Success;
			}

			_mWindow.HandleProxy();
			return Feedback.Success;
		}

		public Feedback WaitFinishCairos(out int runTime)
		{
			var feedback = Feedback.RunFail;
			Functions.AutoRunBtn(_device);

			const CommandPacket cmd = CommandPacket.BattleDungeonResult;
			runTime = 0;

			Functions.ProxyWaitingAdd(_mWindow.LogWizard.Name, cmd, out var infoHeader);

			HandlePauseScreen.Set();

			var infos = Functions.ProxyGetResponse(infoHeader, 30);

			if (infos == null) return Feedback.Failure;

			TryParse(infos["win_lose"].ToString(), out var winlose);
			TryParse(infos["clear_time"]?["current_time"].ToString(), out runTime);
			Debug.WriteLine("WinLose: " + winlose);

			if (winlose == 1)
			{
				Thread.Sleep(10000);

				Functions.DoTap(_device, new Rectangle(113, 60, 24, 25));
				Functions.DoTap(_device, new Rectangle(113, 60, 24, 25));
				Functions.DoTap(_device, new Rectangle(113, 60, 24, 25), 4000);

				feedback = Feedback.Success;
				CrateFunctions.HandleCrateResults(infos["reward"], _smartrune, _device, out var dropDescription);
				if (dropDescription.Count <= 0) _mWindow.NewLog("Drop: None");
				else
				{
					foreach (var text in dropDescription)
						_mWindow.NewLog(text,
							text.Contains("Drop:") ? LogType.Green : text.Contains("Status:") ? LogType.Blue : LogType.Black);

				}
			}
			else
			{
				Thread.Sleep(10000);
				Functions.DoTap(_device, new Rectangle(113, 60, 24, 25));
			}
			HandlePauseScreen.Reset();
			Thread.Sleep(3000);
			Debug.WriteLine("Feedback: " + feedback);
			return feedback;
		}

		public Feedback HandleReplayCairos(int refresh, out bool didRefresh)
		{
			didRefresh = false;
			// Check if Pattern is RunOnce
			if (ActualPattern == RoutinePattern.RunOnce) return Feedback.EndThatRoutine;

			// Tap Replay Button
			Thread.Sleep(1000);
			var rec = new Rectangle(225, 273, 107, 38);
			var template = GetImg("replay_btn");
			var source = (Bitmap)_device.Screenshot.ToImage();
			if (!Functions.CheckSimilarity(source, template, rec, 0.9)) return Feedback.EndThatRoutine;

			// TAP REPLAY BUTTON
			Functions.DoTap(_device, rec);

			rec = new Rectangle(442, 206, 100, 25);
			template = GetImg("recharge_now");
			source = (Bitmap)_device.Screenshot.ToImage();
			// CHECK IF RECHARGE MESSAGE EXISTS
			if (!Functions.CheckSimilarity(source, template, rec, 0.9)) return Feedback.Repeat;

			// CHECK IF UNTIL-ZERO ROUTINE
			if (ActualPattern == RoutinePattern.UntilZeroEnergy) return Feedback.EndThatRoutine;
			// IF NOT, PROCEED TO GET MORE ENERGY
			if (refresh < ActualTimes && Functions.BuyEnergyOrWingsFromMenu(_device, _mWindow.LogWizard.WizardInfo.SocialPointCurrent))
			{
				Functions.DoTap(_device, new Rectangle(225, 273, 107, 38));
				Thread.Sleep(2000);
				didRefresh = true;
			}
			else 
			{
				return Feedback.EndThatRoutine;
			}

			return Feedback.Repeat;

		}

		private static readonly EventWaitHandle HandlePauseScreen = new ManualResetEvent(false);

		private void CheckPause()
		{
			var thread = new Thread(() =>
			{
				while (_routine.IsRunning)
				{
					HandlePauseScreen.WaitOne();
					var shot = (Bitmap)_device.Screenshot.ToImage();
					var rec = new Rectangle(421, 222, 118, 27);
					var temp = GetImg("pause");
					if (Functions.CheckSimilarity(shot, temp, rec, 0.9))
					{
						Functions.DoTap(_device, new Rectangle(455, 239, 227, 162));
					}
					Thread.Sleep(1000);
					rec = new Rectangle(600, 339, 37, 24);
					temp = GetImg("no_btn");
					if (Functions.CheckSimilarity(shot, temp, rec, 0.6))
					{
						Functions.DoTap(_device, rec);
					}
					Thread.Sleep(5000);
				}
			})
			{ IsBackground = true };
			thread.Start();
		}
	}
}
