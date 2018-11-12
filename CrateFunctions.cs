using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using Managed.Adb;
using Newtonsoft.Json.Linq;
using SW_Easy_Way.Config;
using SW_Easy_Way.Interceptor.Infos;
using Image = System.Drawing.Image;

namespace SW_Easy_Way
{
	public static class CrateFunctions
	{
		private static Bitmap GetImg(string img)
		{
			return (Bitmap)Image.FromFile($@"Resources/Crate/{img}.bmp");
		}

		public static void HandleCrateResults(JToken json, Smartrune smartRune, Device device, out List<string> dropDescription)
		{
			var result = json.ToObject<Reward>();
			var crate = result?.Crate;
			dropDescription = new List<string>();
			if (crate?.Rune != null)
			{
				var sellRune = false;
				var runeInfo = CheckRuneInfo(crate.Rune);
				if (smartRune.Enabled)
					sellRune = SmartRuneShouldSell(runeInfo, smartRune);
				Functions.DoTap(device, sellRune ? new Rectangle(361, 423, 82, 29) : new Rectangle(532, 421, 60, 29));
				CheckYesBtn(device);

				MainWindow.Instance.LogSession.StrDropRunes = $"{!sellRune}";
				dropDescription.Add($"Drop: {runeInfo.Stars}* {runeInfo.Rarity} Rune ({runeInfo.Slot})");
				dropDescription.Add($"Status: {(sellRune ? "Sold" : "Kept")}");
				return;
			}
			var rec = new Rectangle(434, 426, 92, 36);
			if (crate?.RandomScroll != null)
			{
				Debug.WriteLine("Random scroll");
				var type = (RandomScroll)crate.RandomScroll.ItemMasterId;
				var qtd = crate.RandomScroll.ItemQuantity;
				rec = new Rectangle(440, 396, 77, 37);
				MainWindow.Instance.LogSession.StrDropScrolls = $"{(int)type}-{qtd}";
				dropDescription.Add($"Drop: {type.GetDescription()} x{qtd}");
			}
			else if (crate?.CraftStuff != null)
			{
				Debug.WriteLine("Craft Stuff");
				var type = (CraftMaterial)crate.CraftStuff.ItemMasterId;
				var qtd = crate.CraftStuff.ItemQuantity;
				MainWindow.Instance.LogSession.StrDropCraft = $"{(int)type}-{qtd}";
				dropDescription.Add($"Drop: {type.GetDescription()} x{qtd}");
			}
			else if (crate?.UnitInfo != null)
			{
				Debug.WriteLine("Unit Info");
				var type = SwMonsters.SwMonstersList.First(i => i.Id == crate.UnitInfo.UnitMasterId);
				var stars = crate.UnitInfo.Class;
				MainWindow.Instance.LogSession.StrDropMonsters = $"{type.Id}-{stars}";
				dropDescription.Add($"Drop: {type.Name} {stars}*");
			}
			else
			{
				using (var sr = new StreamWriter(@"D:/extra-drops.txt"))
				{
					sr.WriteLine(json.ToString());
				}
			}
			// TODO: costume point (shapeshifting stone)
			Debug.WriteLine("Dotap: " + rec);
			Functions.DoTap(device, rec);
		}

		private static bool SmartRuneShouldSell(RuneInfo rune, Smartrune config)
		{
			if (config.Minstars > rune.Stars) return true;

			if (rune.Stars == 6)
			{
				if (rune.Rarity < config.Minrarity6) return true;
			}
			else
			{
				if (rune.Rarity < config.Minrarity5) return true;
			}

			if (rune.Rarity == RuneRarity.Legend) return false;

			if (rune.Slot == 2 || rune.Slot == 4 || rune.Slot == 6)
			{
				if (rune.MainStat == RuneStat.Spd)
				{
					if (config.Min2Spd > rune.Stars) return true;
				}
				else
				{
					if (config.Min246 > rune.Stars) return true;
				}

				if (rune.MainStat == RuneStat.AtkFlat ||
					rune.MainStat == RuneStat.DefFlat ||
					rune.MainStat == RuneStat.HpFlat ||
					rune.MainStat == RuneStat.Resistance ) return true;

				// TODO: FIX THIS PART ON THE CONFIG FILE
				if (rune.Stars == 6 && rune.Rarity >= RuneRarity.Hero) return false;
			}

			var found = false;
			var properMain = config.Runepattern.Any(i => i.Main == rune.MainStat);
			foreach (var pat in config.Runepattern)
			{
				if (properMain && pat.Main != rune.MainStat) continue;

				var d = 0;
				var dm = 0;

				if (pat.Sub1 != RuneStat.None)
				{
					dm++;
					if (rune.Sub1 == pat.Sub1) d++;
					else if (rune.Sub2 == pat.Sub1) d++;
					else if (rune.Sub3 == pat.Sub1) d++;
					else if (rune.Sub4 == pat.Sub1) d++;
				}
				if (pat.Sub2 != RuneStat.None)
				{
					dm++;
					if (rune.Sub1 == pat.Sub2) d++;
					else if (rune.Sub2 == pat.Sub2) d++;
					else if (rune.Sub3 == pat.Sub2) d++;
					else if (rune.Sub4 == pat.Sub2) d++;
				}
				if (pat.Sub3 != RuneStat.None)
				{
					dm++;
					if (rune.Sub1 == pat.Sub3) d++;
					else if (rune.Sub2 == pat.Sub3) d++;
					else if (rune.Sub3 == pat.Sub3) d++;
					else if (rune.Sub4 == pat.Sub3) d++;
				}

				if (dm == d && d != 0)
				{
					found = true;
					break;
				}
			}
			return !found;
		}

		private static RuneInfo CheckRuneInfo(Rune info)
		{
			var rRarity = info.Rank;
			var rSub1 = 0;
			var rSub2 = 0;
			var rSub3 = 0;
			var rSub4 = 0;
			if (rRarity >= 2) rSub1 = info.SecEff[0][0];
			if (rRarity >= 3) rSub2 = info.SecEff[1][0];
			if (rRarity >= 4) rSub3 = info.SecEff[2][0];
			if (rRarity >= 5) rSub4 = info.SecEff[3][0];

			var runeInfo = new RuneInfo {
				Rarity = (RuneRarity)rRarity,
				Slot = info.SlotNo,
				Stars = info.Class,
				MainStat = (RuneStat)info.PriEff[0],
				SubStat = (RuneStat)info.PrefixEff[0],
				Sub1 = (RuneStat)rSub1,
				Sub2 = (RuneStat)rSub2,
				Sub3 = (RuneStat)rSub3,
				Sub4 = (RuneStat)rSub4
			};

			return runeInfo;
		}

		public static void CheckYesBtn(Device device)
		{
			var rec = new Rectangle(371, 311, 49, 29);
			if (Functions.CheckSimilarity((Bitmap)device.Screenshot.ToImage(), GetImg("yes_btn"), rec, 0.90))
			{
				Functions.DoTap(device, rec, 2000);
			}
		}
	}

	public class RuneInfo
	{
		public int Stars { get; set; }
		public int Slot { get; set; }
		public RuneRarity Rarity { get; set; }
		public RuneStat MainStat { get; set; }
		public RuneStat SubStat { get; set; }
		public RuneStat Sub1 { get; set; }
		public RuneStat Sub2 { get; set; }
		public RuneStat Sub3 { get; set; }
		public RuneStat Sub4 { get; set; }
	}

	public enum RuneRarity
	{
		None,
		Normal,
		Magic,
		Rare,
		Hero,
		Legend
	}

	public enum RuneStat
	{
		[Description("None")]
		None,
		[Description("Hp Flat")]
		HpFlat,
		[Description("Hp %")]
		Hp,
		[Description("Atk Flat")]
		AtkFlat,
		[Description("Atk %")]
		Atk,
		[Description("Def Flat")]
		DefFlat,
		[Description("Def %")]
		Def,
		[Description("None")]
		None2,
		[Description("SPD")]
		Spd,
		[Description("Crit Rate")]
		CritRate,
		[Description("Crit Dmg")]
		CritDmg,
		[Description("Res")]
		Resistance,
		[Description("Acc")]
		Accuracy
	}

	public enum RandomScroll
	{
		[Description("Unknown Scroll")]
		UnknownScroll = 1,
		[Description("Mystical Scroll")]
		MysticalScroll = 2,
		[Description("Summoning Stones")]
		SummoningStones = 8
	}

	public enum CraftMaterial
	{
		[Description("Hard Wood")]
		Hardwood = 1001,
		[Description("Tough Leather")]
		ToughLeather = 1002,
		[Description("Solid Rock")]
		SolidRock = 1003,
		[Description("Solid Iron Ore")]
		SolidIronOre = 1004,
		[Description("Shining Mithril")]
		ShiningMithril = 1005,
		[Description("Thick Cloth")]
		ThickCloth = 1006,
		[Description("Rune Piece")]
		RunePiece = 2001,
		[Description("Magic Dust")]
		MagicDust = 3001,
		[Description("Symbol of Harmony")]
		SymbolofHarmony = 4001,
		[Description("Symbol of Transcendence")]
		SymbolofTranscendence = 4002,
		[Description("Symbol of Chaos")]
		SymbolofChaos = 4003,
		[Description("Frozen Water Crystal")]
		FrozenWaterCrystal = 5001,
		[Description("Flaming Fire Crystal")]
		FlamingFireCrystal = 5002,
		[Description("Whirling Wind Crystal")]
		WhirlingWindCrystal = 5003,
		[Description("Shiny Light Crystal")]
		ShinyLightCrystal = 5004,
		[Description("Pitch-black Dark Crystal")]
		PitchblackDarkCrystal = 5005,
		[Description("Condensed Magic Crystal")]
		CondensedMagicCrystal = 6001,
		[Description("Pure Magic Crystal")]
		PureMagicCrystal = 6002,
	}
}
