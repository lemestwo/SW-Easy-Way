using System;
using System.Drawing;
using System.Linq;
using System.Threading;
using Managed.Adb;
using SW_Easy_Way.Interceptor;

namespace SW_Easy_Way.Modules
{
	public class Arena
	{
		private readonly Device _device;
		private readonly MainWindow _mWindow;
		private readonly Routine _routine;

		private bool _hasMore;
		public RivalArena TempRival;

		public Arena(Device device, MainWindow mWindow, Routine routine)
		{
			_device = device;
			_mWindow = mWindow;
			_routine = routine;
		}

		private static Bitmap GetImg(string img)
		{
			return (Bitmap)Image.FromFile($@"Resources/Arena/{img}.bmp");
		}

		public Feedback SelectKind(Activity activity)
		{
			Thread.Sleep(1000);
			if (_routine.QueuePriority[0].Activity == Activity.ArenaRival)
				if (_mWindow.LogWizard.NpcList == null ||
				    _mWindow.LogWizard.NpcList.Where(i => i.NextBattle == 0).ToArray().Length <= 0) return Feedback.EndThatRoutine;
			if (_mWindow.LogWizard.WizardInfo.ArenaEnergy <= 0)
			{
				_mWindow.NewLog("Not enough Wings", LogType.Red);
				return Feedback.EndThatRoutine;
			}
			if (!DoWhileSimilarity(60, GetImg("select_kind"), new Rectangle(297, 130, 53, 18), 0.9)) return Feedback.EndThatRoutine;

			if (activity == Activity.ArenaRival || activity == Activity.ArenaNormal)
			{
				Functions.DoTap(_device, new Rectangle(293, 213, 73, 113), 2000);
				var rec = activity == Activity.ArenaRival ? new Rectangle(778, 295, 104, 36) : new Rectangle(781, 201, 105, 40);
				Functions.DoTap(_device, rec);
				if (!DoWhileSimilarity(60, GetImg("arena_title"), new Rectangle(472, 64, 60, 22), 0.9)) return Feedback.Failure;
			}
			else
			{
				Functions.DoTap(_device, new Rectangle(593, 179, 84, 137));
			}
			return Feedback.Success;
		}

		public Feedback SelectRival()
		{
			var swipe = false;
			var rec = new Rectangle();
			if (_mWindow.LogWizard.NpcList == null) return Feedback.EndThatRoutine;
			var enemyList = (from npc in _mWindow.LogWizard.NpcList where npc.NextBattle == 0 select (RivalArena)npc.WizardId).ToList();
			if (enemyList.Count <= 0) return Feedback.EndThatRoutine;

			foreach (var rival in enemyList)
			{
				switch (rival)
				{
					case RivalArena.Gready:
						rec = new Rectangle(750, 131, 40, 29);
						break;
					case RivalArena.Razak:
						rec = new Rectangle(751, 204, 37, 32);
						break;
					case RivalArena.Taihan:
						rec = new Rectangle(749, 283, 38, 30);
						break;
					case RivalArena.Shai:
						rec = new Rectangle(754, 364, 35, 32);
						break;
					case RivalArena.Morgana:
						rec = new Rectangle(750, 438, 38, 25);
						break;
					case RivalArena.Volta:
						rec = new Rectangle(749, 177, 43, 31);
						swipe = true;
						break;
					case RivalArena.Edmund:
						rec = new Rectangle(752, 261, 33, 26);
						swipe = true;
						break;
					case RivalArena.Kellan:
						rec = new Rectangle(750, 334, 39, 36);
						swipe = true;
						break;
					case RivalArena.Kian:
						rec = new Rectangle(750, 417, 34, 27);
						swipe = true;
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}
				if (!rec.IsEmpty)
				{
					TempRival = rival;
					break;
				}
			}
			if (enemyList.Count > 1) _hasMore = true;
			else if (enemyList.Count == 1) _hasMore = false;

			var recSwipe = new Rectangle(472, 173, 70, 21);
			var recSwipe2 = new Rectangle(491, 406, 62, 23);
			if (swipe)
			{
				var temp = recSwipe2;
				recSwipe2 = recSwipe;
				recSwipe = temp;
			}
			Functions.DoSwipe(recSwipe, recSwipe2, _device);
			Functions.DoSwipe(recSwipe, recSwipe2, _device);

			Functions.ProxyWaitingAdd(null, CommandPacket.GetArenaUnitList, out var infoHeader);
			Functions.DoTap(_device, rec);

			return Functions.ProxyGetResponse(infoHeader, 1) != null ? Feedback.Success : Feedback.EndThatRoutine;
		}

		public Feedback DoMonsterPreparation(int deck)
		{
			Functions.DoTap(_device, new Rectangle(33, 264, 22, 21));
			Rectangle rec;
			switch (deck)
			{
				case 2:
					rec = new Rectangle(139, 241, 183, 19);
					break;
				case 3:
					rec = new Rectangle(132, 321, 200, 23);
					break;
				case 4:
					rec = new Rectangle(139, 401, 190, 23);
					break;
				default:
					rec = new Rectangle(139, 159, 180, 18);
					break;
			}
			// select deck
			Functions.DoTap(_device, rec);

			// battle button
			Functions.ProxyWaitingAdd(_mWindow.LogWizard.Name, CommandPacket.BattleArenaStart, out var infoHeader);
			Functions.DoTap(_device, new Rectangle(767, 363, 88, 29));

			var btnrec = new Rectangle(444, 310, 71, 30);
			if (Functions.CheckSimilarity(Functions.GetShot(_device), GetImg("ok_btn"), btnrec, 0.9))
			{
				Functions.DoTap(_device, btnrec);
				return Feedback.EndThatRoutine;
			}

			return Functions.ProxyGetResponse(infoHeader, 2) != null ? Feedback.Success : Feedback.EndThatRoutine;
		}

		public Feedback WaitFinish()
		{
			Functions.ProxyWaitingAdd(_mWindow.LogWizard.Name, CommandPacket.BattleArenaResult, out var infoHeader);
			Thread.Sleep(6000);
			var rec = new Rectangle(330, 475, 212, 31);
			Functions.DoTap(_device, rec);
			Functions.DoTap(_device, new Rectangle(167, 489, 20, 30));

			if (Functions.ProxyGetResponse(infoHeader, 10) == null) return Feedback.EndThatRoutine;

			foreach (var t in _mWindow.LogWizard.NpcList)
			{
				if (t.WizardId == (int)TempRival) t.NextBattle = 200;
			}
			Thread.Sleep(7000);
			Functions.DoTap(_device, rec);
			Functions.DoTap(_device, rec);
			return _hasMore ? Feedback.Repeat : Feedback.EndThatRoutine;
		}

		private bool DoWhileSimilarity(int seconds, Bitmap temp, Rectangle rec, double sim)
		{
			var start = DateTime.Now;
			var shot = Functions.GetShot(_device);
			while (DateTime.Now.Subtract(start).Seconds <= seconds)
			{
				var result = Functions.CheckSimilarity(shot, temp, rec, sim);
				if (result) return true;
				Thread.Sleep(1000);
			}
			return false;
		}
	}

	public enum RivalArena
	{
		None,
		Gready = 5001,
		Morgana = 5002,
		Edmund = 5003,
		Volta = 5004,
		Taihan = 5006,
		Shai = 5007,
		Razak = 5009,
		Kellan = 5010,
		Kian = 5011
	}
}
