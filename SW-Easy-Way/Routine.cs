using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using SW_Easy_Way.Config;
using SW_Easy_Way.Interceptor.Infos;
using SW_Easy_Way.Modules;
using RivalArena = SW_Easy_Way.Modules.RivalArena;

namespace SW_Easy_Way
{
	public class Routine
	{
		// Instances
		private readonly JsonConfig _config;
		private readonly MainWindow _mWindow;

		// Modules Instances
		private readonly Arena _mArena;
		private readonly Cairos _mCairos;
		private readonly Island _mIsland;
		private readonly Worldboss _mWb;

		// Controllers
		public bool IsRunning { get; set; }

		private int _internalId;
		private int _runTime;

		// Temporary Controller
		private readonly int _failTreshold;
		private int _failCount;
		private int _runsCount;
		private int _refreshCount;

		// Main Controller
		public List<QueuePriority> QueuePriority = new List<QueuePriority>();

		public Routine(string serial, JsonConfig config, MainWindow window)
		{
			// Setting up Instances
			_config = config;
			_mWindow = window;

			var device = Functions.GetDevices(serial)?[0];
			if (device == null)
			{
				_mWindow.NewLog($"No device found. Serial \"{serial}\"", LogType.Red, true);
				return;
			}

			// Setting up Controllers
			// Todo: Fail Threshold
			IsRunning = true;
			_internalId = 1;
			_failTreshold = 5;
			_failCount = 0;
			_runsCount = 0;
			_refreshCount = 0;

			// Setting up Queue List
			if (config.Wb.Enabled)
				QueuePriority.Add(new QueuePriority { Activity = Activity.WorldBoss, Priority = Priority.High, State = State.Waiting });
			if (config.FriendGift.Enabled)
				QueuePriority.Add(new QueuePriority { Activity = Activity.FriendGift, Priority = Priority.High, State = State.Waiting });
			if (config.RivalArena.Enabled)
				QueuePriority.Add(new QueuePriority { Activity = Activity.ArenaRival, Priority = Priority.High, State = State.Waiting });
			if (config.Dragon.Enabled)
				QueuePriority.Add(new QueuePriority { Activity = Activity.Dragon, Priority = Priority.Normal, State = State.Waiting });
			if (config.Giant.Enabled)
				QueuePriority.Add(new QueuePriority { Activity = Activity.Giant, Priority = Priority.Normal, State = State.Waiting });
			if (config.Necro.Enabled)
				QueuePriority.Add(new QueuePriority { Activity = Activity.Necro, Priority = Priority.Normal, State = State.Waiting });
			UpdateQueuePriority();

			// Setting up Modules
			_mArena = new Arena(device, _mWindow, this);
			_mIsland = new Island(device, _mWindow);
			_mCairos = new Cairos(device, _config.Smartrune, _mWindow, this);
			_mWb = new Worldboss(device, this, _config.Wb, _config.Smartrune);

			_mWindow.NewLog("Starting new routine");
		}

		private void UpdateQueuePriority()
		{
			var any = QueuePriority.Any(item => item.State == State.Running);
			if (any) return;

			var map = new[] { 1, 2, 3 };
			var newqueue = QueuePriority.OrderBy(x => map[(int)x.Priority]).ToList();
			QueuePriority = newqueue;
		}

		public void DoNextAction()
		{
			var timer = Stopwatch.StartNew();
			if (!QueuePriority.Any() || !IsRunning)
			{
				IsRunning = false;
				_mWindow.NewLog("This routine is done", LogType.Green);
				_mWindow.SetBtnStartToAction();
				return;
			}
			if (QueuePriority[0].State == State.Done)
			{
				QueuePriority.RemoveAt(0);
				UpdateQueuePriority();
				_internalId = 1;
				return;
			}
			if (_failCount >= _failTreshold)
			{
				QueuePriority.RemoveAt(0);
				UpdateQueuePriority();
				_mWindow.NewLog($"Failed {_failCount} runs, jumping to next routine", LogType.Red);
				return;
			}
			if (QueuePriority[0].State != State.Running)
			{
				QueuePriority[0].State = State.Running;
				if (_runsCount > 0) _mWindow.NewLog("Total runs: " + _runsCount);
				_failCount = 0;
				_runsCount = 0;
				_refreshCount = 0;
				_internalId = 1;
				_mWindow.NewLog($"Loading \"{QueuePriority[0].Activity.GetDescription()}\" module", LogType.Blue);
				_mCairos.ActualActivity = QueuePriority[0].Activity;
				switch (QueuePriority[0].Activity)
				{
					case Activity.Dragon:
						_mCairos.ActualFloor = _config.Dragon.Floor;
						_mCairos.ActualPattern = _config.Dragon.Pattern;
						_mCairos.ActualTimes = _config.Dragon.Repeat;
						break;
					case Activity.Giant:
						_mCairos.ActualFloor = _config.Giant.Floor;
						_mCairos.ActualPattern = _config.Giant.Pattern;
						_mCairos.ActualTimes = _config.Giant.Repeat;
						break;
					case Activity.Necro:
						_mCairos.ActualFloor = _config.Necro.Floor;
						_mCairos.ActualPattern = _config.Necro.Pattern;
						_mCairos.ActualTimes = _config.Necro.Repeat;
						break;
					case Activity.ElementalHall:
						_mCairos.ActualFloor = _config.ElementalHall.Floor;
						_mCairos.ActualPattern = _config.ElementalHall.Pattern;
						_mCairos.ActualTimes = _config.ElementalHall.Repeat;
						break;
					case Activity.MagicHall:
						_mCairos.ActualFloor = _config.MagicHall.Floor;
						_mCairos.ActualPattern = _config.MagicHall.Pattern;
						_mCairos.ActualTimes = _config.MagicHall.Repeat;
						break;
					case Activity.WorldBoss:
						break;
					case Activity.FriendGift:
						break;
					case Activity.ArenaRival:
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}
			}

			switch (QueuePriority[0].Activity)
			{
				case Activity.Dragon:
				case Activity.Giant:
				case Activity.Necro:
					CairosRoutine();
					break;
				case Activity.ElementalHall:
				case Activity.MagicHall:
					CairosRoutine(true);
					break;
				case Activity.WorldBoss:
					WorldBossRoutine();
					break;
				case Activity.FriendGift:
					break;
				case Activity.ArenaRival:
					ArenaRivalRoutine();
					break;
				case Activity.ArenaNormal:
					break;
				case Activity.ArenaWorld:
					break;
				default:
					IsRunning = false;
					break;
			}

			if (_runTime > 0)
			{
				var runTimer = TimeSpan.FromMilliseconds(_runTime);
				_mWindow.NewLog($"Run time: {runTimer:mm\\:ss}");
				_runTime = 0;
			}
			timer.Stop();
			var timespan = timer.Elapsed;
			var sleep = 4 - timespan.Seconds < 0 ? 1 : 4 - timespan.Seconds;
			Thread.Sleep(sleep * 1000);
		}

		private void ArenaRivalRoutine()
		{
			var done = Feedback.EndThatRoutine;

			switch (_internalId)
			{
				case 1:
					if (_mIsland.ReturnToIsland() == Feedback.Success)
						if (_mIsland.IslandToWord() == Feedback.Success)
							if (_mIsland.GoToPlace(QueuePriority[0].Activity) == Feedback.Success)
								done = _mArena.SelectKind(QueuePriority[0].Activity);
					break;
				case 2:
					done = _mArena.SelectRival();
					break;
				case 3:
					done = _mArena.DoMonsterPreparation(_config.RivalArena.Deck);
					break;
				case 4:
					done = _mArena.WaitFinish();
					break;
			}

			switch (done)
			{
				case Feedback.Success:
					_internalId++;
					break;
				case Feedback.Repeat:
					_internalId = 2;
					break;
				case Feedback.EndThatRoutine:
					_mArena.TempRival = RivalArena.None;
					QueuePriority[0].State = State.Done;
					break;
				case Feedback.Failure:
					IsRunning = false;
					break;
				default:
					IsRunning = false;
					break;
			}
		}

		private void CairosRoutine(bool essence = false)
		{
			// TODO: FAIL THRESHOLD 2~3 TIMES
			var done = Feedback.Restart;
			switch (_internalId)
			{
				case 1:
					if (_mIsland.ReturnToIsland() == Feedback.Success)
						if (_mIsland.IslandToWord() == Feedback.Success)
							if (_mIsland.GoToPlace(QueuePriority[0].Activity) == Feedback.Success)
								if (_mCairos.SelectCairosDungeon() == Feedback.Success)
									if (_mCairos.SelectCairosFloor() == Feedback.Success)
									{
										done = _mCairos.DoMonstersPreparations();
										Thread.Sleep(7000);
									}
					break;
				case 2:
					_mWindow.NewLog($"Run {_runsCount + 1} is starting");
					done = Feedback.Success;
					break;
				case 3:
					done = _mCairos.WaitFinishCairos(out _runTime);
					if (done == Feedback.Success || done == Feedback.RunFail)
					{
						var runStatus = "false";
						if (done == Feedback.Success) runStatus = "true";
						switch (QueuePriority[0].Activity)
						{
							case Activity.Dragon:
								_mWindow.LogSession.StrDragonRuns = runStatus;
								break;
							case Activity.Giant:
								_mWindow.LogSession.StrGiantRuns = runStatus;
								break;
							case Activity.Necro:
								_mWindow.LogSession.StrNecroRuns = runStatus;
								break;
							case Activity.ElementalHall:
								_mWindow.LogSession.StrElemHallRuns = runStatus;
								break;
							case Activity.MagicHall:
								_mWindow.LogSession.StrMagicHallRuns = runStatus;
								break;
						}
					}
					_runsCount++;
					break;
				case 4:
					done = _mCairos.HandleReplayCairos(_refreshCount, out var didRefresh);
					if (didRefresh) _refreshCount++;
					break;
				default:
					done = Feedback.Failure;
					break;
			}

			switch (done)
			{
				case Feedback.Success:
					_internalId++;
					break;
				case Feedback.Repeat:
					_internalId = 2;
					break;
				case Feedback.EndThatRoutine:
					QueuePriority[0].State = State.Done;
					break;
				case Feedback.RunFail:
					_failCount++;
					_internalId++;
					break;
				case Feedback.Failure:
					IsRunning = false;
					break;
				case Feedback.Restart:
					QueuePriority[0].State = State.Waiting;
					_mWindow.NewLog("Restarting routine, something went wrong.", LogType.Red);
					break;
				default:
					IsRunning = false;
					break;
			}
			Debug.WriteLine($"Routine: {done} {_internalId}");
		}

		private void WorldBossRoutine()
		{
			// TODO: FAIL THRESHOLD 2~3 TIMES
			var done = Feedback.Failure;
			switch (_internalId)
			{
				case 1:
					if (_mIsland.ReturnToIsland() == Feedback.Success)
						done = _mIsland.IslandToWord();
					break;
				case 2:
					done = _mIsland.GoToPlace(QueuePriority[0].Activity);
					break;
				case 3:
					done = _mWb.CheckWorldBoss();
					break;
				case 4:
					done = _mWb.DoMonsterPreparation();
					// TODO: IMPLEMENT LOADING CONFIGURABLE DELAY
					if (done == Feedback.Success)
						Thread.Sleep(6000);
					break;
				case 5:
					_mWindow.NewLog("Starting new run");
					done = _mWb.WaitForFinish();
					_runsCount++;
					break;
				case 6:
					// TODO: PRINT ALL RESULTS ON LOG
					done = _mWb.HandleResults();
					break;
				default:
					done = Feedback.Failure;
					break;
			}

			switch (done)
			{
				case Feedback.Success:
					_internalId++;
					break;
				case Feedback.Repeat:
					_internalId = 3;
					break;
				case Feedback.EndThatRoutine:
					QueuePriority[0].State = State.Done;
					break;
				case Feedback.NotEnoughMonsters:
					QueuePriority[0].State = State.Done;
					_mWindow.NewLog("Not enough monsters", LogType.Red);
					break;
				case Feedback.Failure:
					IsRunning = false;
					break;
				default:
					IsRunning = false;
					break;
			}
		}
	}

	public enum Feedback
	{
		Success,
		Failure,
		RunFail,
		EndThatRoutine,
		Repeat,
		Restart,
		NotEnoughMonsters
	}

	public class QueuePriority
	{
		public Activity Activity { get; set; }
		public Priority Priority { get; set; }
		public State State { get; set; }
	}

	public enum Activity
	{
		[Description("World Boss")]
		WorldBoss,
		[Description("Dragon's Lair")]
		Dragon,
		[Description("Giant's Keep")]
		Giant,
		[Description("Necropolis")]
		Necro,
		[Description("Elemental Hall")]
		ElementalHall,
		[Description("Hall of Magic")]
		MagicHall,
		[Description("Friend Gift")]
		FriendGift,
		[Description("Arena - Rival")]
		ArenaRival,
		[Description("Arena - Battle")]
		ArenaNormal,
		[Description("world Arena")]
		ArenaWorld
	}

	public enum Priority
	{
		High,
		Normal,
		Low
	}

	public enum State
	{
		Waiting,
		Running,
		Done,
		// Todo: Infinite routine
		DoneCanRepeat
	}
}
