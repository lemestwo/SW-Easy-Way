using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Threading;
using AForge.Imaging;
using Managed.Adb;
using Newtonsoft.Json.Linq;
using SW_Easy_Way.Interceptor;
using XnaFan.ImageComparison;
using Image = System.Drawing.Image;

namespace SW_Easy_Way
{
	public static class Functions
	{
		public static List<Device> GetDevices(string serial = null)
		{
			List<Device> device = null;
			try
			{
				if (serial != null)
				{
					device = AdbHelper.Instance.GetDevices(AndroidDebugBridge.SocketAddress)
						.Where(d => d.SerialNumber == serial)
						.ToList();
				}
				else
				{
					device = AdbHelper.Instance.GetDevices(AndroidDebugBridge.SocketAddress)
						.Where(d => d.State == DeviceState.Online)
						.ToList();
				}
			}
			catch
			{
				// ignored
			}

			return device ?? new List<Device>();
		}

		public static bool CheckSimilarity(Bitmap source, Bitmap template, Rectangle rec, double similarity, bool grays = false, bool grayt = false, bool debug = false)
		{
			source = CropImage(source, rec);
			if (grays) source = MakeGrayscale3(source);
			if (grayt) template = MakeGrayscale3(template);
			var similar = 1 - source.PercentageDifference(template);
			if (debug) Debug.WriteLine(similar);
			return similar >= similarity;
		}

		public static float CheckSimilarity(Bitmap source, Bitmap template, Rectangle rec, bool debug = false)
		{
			source = MakeGrayscale3(CropImage(source, rec));
			template = MakeGrayscale3(template);
			var similar = 1 - source.PercentageDifference(template);
			if (debug) Debug.WriteLine(similar);
			return similar;
		}

		public static TemplateMatch[] CheckSimilarityDeep(Bitmap source, Rectangle rec, Bitmap template, float similarity, bool gray = false)
		{
			source = CropImage(source, rec);
			if (gray)
			{
				source = MakeGrayscale3(source);
				template = MakeGrayscale3(template);
			}
			source = ConvertToFormat(source, PixelFormat.Format24bppRgb);
			template = ConvertToFormat(template, PixelFormat.Format24bppRgb);
			var tm = new ExhaustiveTemplateMatching(similarity);
			var matchings = tm.ProcessImage(source, template);
			return matchings.Any() ? matchings : null;
		}

		public static Bitmap GetShot(Device device)
		{
			return (Bitmap)device.Screenshot.ToImage();
		}

		private static string GetPath(string img)
		{
			return $@"Resources/Others/{img}.bmp";
		}

		public static void ProxyWaitingAdd(string name, CommandPacket command, out Tuple<string, CommandPacket> header)
		{
			header = new Tuple<string, CommandPacket>(name, command);
			MainWindow.Instance.PacketListWaiting.Add(header);
		}

		public static JToken ProxyGetResponse(Tuple<string, CommandPacket> header, int minutes)
		{
			var start = DateTime.Now;
			JToken infoBody = null;
			while (DateTime.Now.Subtract(start).Minutes <= minutes)
			{
				if (MainWindow.Instance.PacketListFound.TryGetValue(header, out infoBody))
				{
					if (infoBody != null)
					{
						MainWindow.Instance.PacketListFound.Remove(header);
						break;
					}
				}
				Thread.Sleep(200);
			}
			return infoBody;
		}

		public static bool CheckNoEnergyMessage(Device device)
		{
			var rec = new Rectangle(483, 178, 100, 27);
			var template = (Bitmap)Image.FromFile(GetPath("no_energy"));
			var source = (Bitmap)device.Screenshot.ToImage();
			return CheckSimilarity(source, template, rec, 0.9);
		}

		private static bool EnergyBoxTap(Device device, int count = 0)
		{
			var rec = new Rectangle(311, 176, 26, 34);
			var template = (Bitmap)Image.FromFile(GetPath("energy_icon"));
			var source = (Bitmap)device.Screenshot.ToImage();
			if (CheckSimilarity(source, template, rec, 0.9))
			{
				for (var i = 0; i < 5; i++)
				{
					DoTap(device, new Rectangle(578, 181, 62, 20));
					Thread.Sleep(1000);
					count++;
				}
				EnergyBoxTap(device, count);
			}

			return count > 0;
		}

		public static bool BuyEnergyOrWingsFromMenu(Device device, int currGift, bool wings = false)
		{
			// FIRST - TRY GIFT BOX
			DoTap(device, new Rectangle(523, 318, 98, 34));
			var giftBoxEnergy = EnergyBoxTap(device);
			if (giftBoxEnergy)
			{
				DoTap(device, new Rectangle(683, 95, 18, 21));
				return true;
			}
			// GET TO SHOP IF NO ENERGY ON BOX
			DoTap(device, new Rectangle(354, 322, 76, 26));
			// SELECT WINGS OR ENERGY BASED ON CONFIGURATION
			var rec = wings ? new Rectangle(577, 246, 66, 100) : new Rectangle(348, 217, 92, 136);
			DoTap(device, rec);

			// BUY
			rec = new Rectangle(327, 199, 102, 22);
			var template = (Bitmap)Image.FromFile(GetPath("purchase"));
			var source = (Bitmap)device.Screenshot.ToImage();
			if (!CheckSimilarity(source, template, rec, 0.9)) return false;

			// PRESS YES BUTTON
			DoTap(device, new Rectangle(373, 319, 54, 21), 3000);
			rec = new Rectangle(473, 200, 114, 18);
			template = (Bitmap)Image.FromFile(GetPath("success"));
			source = (Bitmap)device.Screenshot.ToImage();
			if (!CheckSimilarity(source, template, rec, 0.9)) return false;

			DoTap(device, new Rectangle(454, 309, 57, 30));
			DoTap(device, new Rectangle(444, 450, 79, 23));
			MainWindow.Instance.LogSession.TotalRefreshs++;
			Debug.WriteLine("Buy Energy: Success");
			return true;
		}

		public static void AutoRunBtn(Device device)
		{
			var rec = new Rectangle(165, 489, 24, 32);
			var template = (Bitmap)Image.FromFile(GetPath("auto_run_btn"));
			var source = (Bitmap)device.Screenshot.ToImage();
			if (!CheckSimilarity(source, template, rec, 0.85, true, true, true)) return;

			DoTap(device, rec);
		}

		public static void DoAdbCommand(string command, Device device)
		{
			try
			{
				AdbHelper.Instance.ExecuteRemoteCommand(AndroidDebugBridge.SocketAddress, command, device, NullOutputReceiver.Instance);
			}
			catch (Exception e)
			{
				Debug.WriteLine(e);
			}
		}

		public static void DoTap(Device device, Rectangle rec, int sleep = 1000)
		{
			var p = GetRandomPointFromRec(rec);
			DoAdbCommand($"input tap {p.X} {p.Y}", device);
			Thread.Sleep(sleep);
		}

		public static void DoSwipe(Rectangle rec1, Rectangle rec2, Device device, int sleep = 500)
		{
			var p = GetRandomPointFromRec(rec1);
			var p2 = GetRandomPointFromRec(rec2);
			DoAdbCommand($"input swipe {p.X} {p.Y} {p2.X} {p2.Y}", device);
			Thread.Sleep(sleep);
		}

		public static void DoStartApp(Device device, int sleep = 1000)
		{
			DoAdbCommand("am start -n com.com2us.smon.normal.freefull.google.kr.android.common/.SubActivity", device);
			Thread.Sleep(sleep);
		}

		public static Point GetRandomPointFromRec(Rectangle rec)
		{
			var random = new Random();
			return new Point(random.Next(rec.X, rec.X + rec.Width), random.Next(rec.Y, rec.Y + rec.Height));
		}

		internal static Bitmap ConvertToFormat(this Image image, PixelFormat format)
		{
			var copy = new Bitmap(image.Width, image.Height, format);
			using (var gr = Graphics.FromImage(copy))
			{
				gr.DrawImage(image, new Rectangle(0, 0, copy.Width, copy.Height));
			}
			return copy;
		}

		public static Bitmap MakeGrayscale3(Bitmap original)
		{
			var newBitmap = new Bitmap(original.Width, original.Height);
			var g = Graphics.FromImage(newBitmap);
			var colorMatrix = new ColorMatrix(
				new[]
				{
					new[] {.3f, .3f, .3f, 0, 0},
					new[] {.59f, .59f, .59f, 0, 0},
					new[] {.11f, .11f, .11f, 0, 0},
					new float[] {0, 0, 0, 1, 0},
					new float[] {0, 0, 0, 0, 1}
				});
			var attributes = new ImageAttributes();
			attributes.SetColorMatrix(colorMatrix);
			g.DrawImage(original, new Rectangle(0, 0, original.Width, original.Height),
				0, 0, original.Width, original.Height, GraphicsUnit.Pixel, attributes);
			g.Dispose();
			return newBitmap;
		}

		internal static Bitmap CropImage(Image source, Rectangle section)
		{
			var bmp = new Bitmap(section.Width, section.Height);
			var g = Graphics.FromImage(bmp);
			g.DrawImage(source, 0, 0, section, GraphicsUnit.Pixel);
			return bmp;
		}

		public static string GetDescription(this Enum value)
		{
			var type = value.GetType();
			var name = Enum.GetName(type, value);
			if (name != null)
			{
				var field = type.GetField(name);
				if (field != null)
				{
					if (Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute)) is DescriptionAttribute attr)
					{
						return attr.Description;
					}
				}
			}
			return null;
		}
	}
}
