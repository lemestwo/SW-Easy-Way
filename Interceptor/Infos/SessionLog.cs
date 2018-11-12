using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using SW_Easy_Way.Config;
using static System.Boolean;

namespace SW_Easy_Way.Interceptor.Infos
{
	public class SessionLog : INotifyPropertyChanged
	{
		public string Serial { get; set; }
		public string Name { get; set; }
		public int Id { get; set; }

		public ObservableCollection<bool> DropRunes { get; set; } = new ObservableCollection<bool>();
		public ObservableCollection<Tuple<int, int>> DropMonsters { get; set; } = new ObservableCollection<Tuple<int, int>>();
		public ObservableCollection<Tuple<int, int>> DropScrolls { get; set; } = new ObservableCollection<Tuple<int, int>>();
		public ObservableCollection<Tuple<int, int>> DropCraft { get; set; } = new ObservableCollection<Tuple<int, int>>();
		public ObservableCollection<Tuple<ElementalType, int, int, int>> DropEssences { get; set; } = new ObservableCollection<Tuple<ElementalType, int, int, int>>();
		public int DropShapeStone { get; set; }
		public int DropEnergy { get; set; }
		public int DropCrystals { get; set; }
		public int DropWings { get; set; }
		public int TotalRefreshs { get; set; }
		public ObservableCollection<bool> GiantRuns { get; set; } = new ObservableCollection<bool>();
		public ObservableCollection<bool> DragonRuns { get; set; } = new ObservableCollection<bool>();
		public ObservableCollection<bool> NecroRuns { get; set; } = new ObservableCollection<bool>();
		public ObservableCollection<bool> MagicHallRuns { get; set; } = new ObservableCollection<bool>();
		public ObservableCollection<bool> ElemHallRuns { get; set; } = new ObservableCollection<bool>();
		public ObservableCollection<bool> SecretDungeonRuns { get; set; } = new ObservableCollection<bool>();
		public ObservableCollection<bool> ScenarioRuns { get; set; } = new ObservableCollection<bool>();
		public ObservableCollection<bool> WorldBossRuns { get; set; } = new ObservableCollection<bool>();
		public ObservableCollection<bool> NoArenaRuns { get; set; } = new ObservableCollection<bool>();
		public ObservableCollection<bool> RtaArenaRuns { get; set; } = new ObservableCollection<bool>();
		public ObservableCollection<bool> RiArenaRuns { get; set; } = new ObservableCollection<bool>();

		public string StrDropRunes {
			get => $"{DropRunes.Count(i => i) + DropRunes.Count(f => f == false)} ";
			set {
				TryParse(value, out var v);
				DropRunes.Add(v);
				OnPropertyChanged();
			}
		}
		public string StrDropMonsters {
			get => $"{DropMonsters.Count} ";
			set {
				var final = value.Split("-".ToCharArray());
				DropMonsters.Add(new Tuple<int, int>(int.Parse(final[0]), int.Parse(final[1])));
				OnPropertyChanged();
			}
		}
		public string StrDropScrolls {
			get => $"{DropScrolls.Aggregate(0, (current, drop) => current + drop.Item2)} ";
			set {
				var final = value.Split("-".ToCharArray());
				DropScrolls.Add(new Tuple<int, int>(int.Parse(final[0]), int.Parse(final[1])));
				OnPropertyChanged();
			}
		}
		public string StrDropCraft {
			get => $"{DropCraft.Aggregate(0, (current, drop) => current + drop.Item2)} ";
			set {
				var final = value.Split("-".ToCharArray());
				DropCraft.Add(new Tuple<int, int>(int.Parse(final[0]), int.Parse(final[1])));
				OnPropertyChanged();
			}
		}
		public string StrDropEssences {
			get => $"{DropEssences.Aggregate(0, (current, drop) => current + drop.Item2 + drop.Item3 + drop.Item4)} ";
			set {
				var final = value.Split("-".ToCharArray());
				DropEssences.Add(
					new Tuple<ElementalType, int, int, int>((ElementalType)int.Parse(final[0]), 
						int.Parse(final[1]), int.Parse(final[2]), int.Parse(final[3])));
				OnPropertyChanged();
			}
		}

		public string StrGiantRuns {
			get {
				var w = GiantRuns.Count(i => i);
				var l = GiantRuns.Count - w;
				return $"{w + l} Runs ({w}-{l})";
			}
			set {
				TryParse(value, out var v);
				GiantRuns.Add(v);
				OnPropertyChanged();
			}
		}
		public string StrDragonRuns {
			get {
				var w = DragonRuns.Count(i => i);
				var l = DragonRuns.Count - w;
				return $"{w + l} Runs ({w}-{l})";
			}
			set {
				TryParse(value, out var v);
				DragonRuns.Add(v);
				OnPropertyChanged();
			}
		}
		public string StrNecroRuns {
			get {
				var w = NecroRuns.Count(i => i);
				var l = NecroRuns.Count - w;
				return $"{w + l} Runs ({w}-{l})";
			}
			set {
				TryParse(value, out var v);
				NecroRuns.Add(v);
				OnPropertyChanged();
			}
		}
		public string StrMagicHallRuns {
			get {
				var w = MagicHallRuns.Count(i => i);
				var l = MagicHallRuns.Count - w;
				return $"{w + l} Runs ({w}-{l})";
			}
			set {
				TryParse(value, out var v);
				MagicHallRuns.Add(v);
				OnPropertyChanged();
			}
		}
		public string StrElemHallRuns {
			get {
				var w = ElemHallRuns.Count(i => i);
				var l = ElemHallRuns.Count - w;
				return $"{w + l} Runs ({w}-{l})";
			}
			set {
				TryParse(value, out var v);
				ElemHallRuns.Add(v);
				OnPropertyChanged();
			}
		}
		public string StrSecretDungeonRuns {
			get {
				var w = SecretDungeonRuns.Count(i => i);
				var l = SecretDungeonRuns.Count - w;
				return $"{w + l} Runs ({w}-{l})";
			}
			set {
				TryParse(value, out var v);
				SecretDungeonRuns.Add(v);
				OnPropertyChanged();
			}
		}
		public string StrScenarioRuns {
			get {
				var w = ScenarioRuns.Count(i => i);
				var l = ScenarioRuns.Count - w;
				return $"{w + l} Runs ({w}-{l})";
			}
			set {
				TryParse(value, out var v);
				ScenarioRuns.Add(v);
				OnPropertyChanged();
			}
		}
		public string StrWorldBossRuns {
			get {
				var w = WorldBossRuns.Count(i => i);
				var l = WorldBossRuns.Count - w;
				return $"{w + l} Runs ({w}-{l})";
			}
			set {
				TryParse(value, out var v);
				WorldBossRuns.Add(v);
				OnPropertyChanged();
			}
		}
		public string StrNoArenaRuns {
			get {
				var w = NoArenaRuns.Count(i => i);
				var l = NoArenaRuns.Count - w;
				return $"{w + l} Runs ({w}-{l})";
			}
			set {
				TryParse(value, out var v);
				NoArenaRuns.Add(v);
				OnPropertyChanged();
			}
		}
		public string StrRtaArenaRuns {
			get {
				var w = RtaArenaRuns.Count(i => i);
				var l = RtaArenaRuns.Count - w;
				return $"{w + l} Runs ({w}-{l})";
			}
			set {
				TryParse(value, out var v);
				RtaArenaRuns.Add(v);
				OnPropertyChanged();
			}
		}
		public string StrRiArenaRuns {
			get {
				var w = RiArenaRuns.Count(i => i);
				var l = RiArenaRuns.Count - w;
				return $"{w + l} Runs ({w}-{l})";
			}
			set {
				TryParse(value, out var v);
				RiArenaRuns.Add(v);
				OnPropertyChanged();
			}
		}

		public SessionLog()
		{
			CleanSession();
		}

		public void CleanSession()
		{
			Serial = "";
			Name = "";
			Id = 0;

			DropRunes.Clear();
			DropMonsters.Clear();
			DropScrolls.Clear();
			DropCraft.Clear();
			DropEssences.Clear();
			DropShapeStone = 0;
			DropEnergy = 0;
			DropCrystals = 0;
			DropWings = 0;
			TotalRefreshs = 0;

			GiantRuns.Clear();
			DragonRuns.Clear();
			NecroRuns.Clear();
			MagicHallRuns.Clear();
			ElemHallRuns.Clear();
			SecretDungeonRuns.Clear();
			ScenarioRuns.Clear();
			WorldBossRuns.Clear();
			NoArenaRuns.Clear();
			RtaArenaRuns.Clear();
			RiArenaRuns.Clear();
		}

		public event PropertyChangedEventHandler PropertyChanged;

		protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}
