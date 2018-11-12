using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Remoting.Contexts;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using Managed.Adb;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using SW_Easy_Way.Config;
using SW_Easy_Way.Interceptor;
using SW_Easy_Way.Interceptor.Infos;
using Brushes = System.Windows.Media.Brushes;

namespace SW_Easy_Way
{
	public partial class MainWindow : INotifyPropertyChanged
	{
		public static MainWindow Instance;
		private readonly bool _debug;
		private const string ProgramVersion = "01.00";

		private readonly string _profilePath = @"Config/Profiles.json";
		private readonly Profiles _profiles;

		private Thread _proxyThread;
		private Proxy _proxyProccess;

		private Thread _routineThread;
		private Routine _routineProccess;

		public List<Tuple<string, CommandPacket>> PacketListWaiting { get; set; } = new List<Tuple<string, CommandPacket>>();
		public Dictionary<Tuple<string, CommandPacket>, JToken> PacketListFound { get; set; } = new Dictionary<Tuple<string, CommandPacket>, JToken>();


		// MVVM Binding
		private ObservableCollection<string> _devicesList = new ObservableCollection<string>();
		private string _selectedDevice;
		private string _selectedDeviceSerial;
		private string _selectedDeviceName;
		private bool _isFieldsEnabled;
		private JsonConfig _dataBindingConfig;
		private SessionLog _logSession;
		private Brush _botStatusColor;
		private string _botStatus;
		private Wizard _logWizard;

		public MainWindow()
		{
			InitializeComponent();

			LogSession = new SessionLog();
			LogWizard = new Wizard();
			Instance = this;

			NewLog($"Version: {ProgramVersion}", LogType.Green);
			_debug = true;

			_profiles = JsonConvert.DeserializeObject<Profiles>(File.ReadAllText(_profilePath));
			SwMonsters.SwMonstersList = JsonConvert.DeserializeObject<List<SwMonsters>>(File.ReadAllText(@"Interceptor/Infos/Monsters.json"));

			HandleProxy();
			RefreshDevicesList(null, null);

			IsFieldsEnabled = false;
			SetBotStatus("Waiting.");
			DataContext = this;
		}

		public void HandleNewPacket(JToken json)
		{
			if (!string.IsNullOrEmpty(LogWizard.Name))
			{
				using (var sr = json.CreateReader()) JsonSerializer.CreateDefault().Populate(sr, LogWizard);
			}

			if (PacketListWaiting.Count <= 0) return;
			foreach (var tuple in PacketListWaiting)
			{
				var h1 = json["wizard_info"]?["wizard_name"].ToString();
				var h2 = (CommandPacket)Enum.Parse(typeof(CommandPacket), json["command"].ToString());
				var header = new Tuple<string, CommandPacket>(h1, h2);
				if (!Equals(tuple, header)) continue;

				PacketListFound.Add(header, json);
				PacketListWaiting.RemoveAll(i => Equals(i, header));
				break;
			}
		}

		public void HandleProxy()
		{
			if (_proxyThread != null && _proxyThread.IsAlive)
			{
				_proxyProccess.Stop();
				_proxyProccess = null;
				_proxyThread.Abort();
			}

			PacketListWaiting.Clear();
			PacketListFound.Clear();

			_proxyThread = new Thread(() => {
				_proxyProccess = new Proxy { IpAddress = IPAddress.Any, Port = 8080 };
				_proxyProccess.Start();
				while (true)
				{
					Thread.Sleep(100);
				}
			}) { IsBackground = true };
			_proxyThread.Start();
		}

		public void SetBtnStartToAction()
		{
			Dispatcher.Invoke(() => {
				BtnStart_Click(BtnStart, null);
			});
		}

		private void BtnStart_Click(object sender, RoutedEventArgs e)
		{
			var btn = (Button)sender;
			if (btn.Content.ToString() == "START")
			{
				BtnDisableTime(sender);
				if (_routineThread != null && _routineThread.IsAlive)
				{
					NewLog("Error starting new routine, please, restart the application and try again", LogType.Red);
					return;
				}
				if (string.IsNullOrEmpty(SelectedDeviceName))
				{
					NewLog("Empty account name", LogType.Red);
					return;
				}
				// Reset and define Session Logs
				LogSession.CleanSession();
				LogSession.Name = SelectedDeviceName;
				LogSession.Serial = DataBindingConfig.Serial;
				// Update Json Config with Name Textbox
				UpdateNameFromSerial();
				SaveProfile();
				// Starting Routine Thread
				_routineThread = new Thread(() => {
					try
					{
						LogWizard.Name = SelectedDeviceName;
						_routineProccess = new Routine(DataBindingConfig.Serial, DataBindingConfig, this);
						while (_routineProccess.IsRunning)
						{
							_routineProccess.DoNextAction();
							Thread.Sleep(100);
						}
					}
					catch (Exception ex)
					{
						Debug.WriteLine(ex);
					}
					finally
					{
						Dispatcher.Invoke(() => {
							LogWizard = new Wizard();
							_routineProccess.IsRunning = false;
							_routineProccess = null;
						});
					}

				});
				_routineThread.Start();
				SetBotStatus("Running.", new SolidColorBrush(Colors.ForestGreen));
				btn.Content = "STOP";
			}
			else
			{
				BtnDisableTime(sender, 10000);
				try
				{
					_routineThread.Abort();
					_routineThread = null;
				}
				catch (Exception ex)
				{
					Debug.WriteLine(ex);
					throw;
				}
				btn.Content = "START";
				NewLog("Routine stopped with success", LogType.Green);
			}
		}

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			var dev = AdbHelper.Instance.GetDevices(AndroidDebugBridge.SocketAddress)
				.Where(d => d.State == DeviceState.Online)
				.ToList()
				.First();
			dev.Screenshot.ToImage().Save("D:/SW-Pic/" + DateTime.Now.Ticks + ".png");
		}

		private void UpdateNameFromSerial()
		{
			foreach (var jsonConfig in _profiles.JsonConfig)
			{
				if (jsonConfig.Serial != SelectedDeviceSerial) continue;

				jsonConfig.WizardName = SelectedDeviceName;
				break;
			}
		}

		private JsonConfig ReturnProfileFromSerial(string serial)
		{
			if (string.IsNullOrEmpty(serial)) return null;

			if (_profiles.JsonConfig.Any(i => i.Serial == serial)) return _profiles.JsonConfig.First(u => u.Serial == serial);

			var newprofile = new JsonConfig {
				Serial = serial
			};
			var prof = _profiles.JsonConfig.ToList();
			prof.Add(newprofile);
			_profiles.JsonConfig = prof.ToArray();

			using (var file = File.CreateText(_profilePath))
			{
				var js = new JsonSerializer();
				js.Serialize(file, _profiles);
			}
			NewLog($"Profile for \"{serial}\" created", LogType.Green);
			return _profiles.JsonConfig.First(u => u.Serial == serial);
		}

		private void TextBox_OnPreviewTextInput(object sender, TextCompositionEventArgs e)
		{
			var regex = new Regex("[^0-9]+");
			int.TryParse(e.Text, out var number);
			e.Handled = regex.IsMatch(e.Text) && number >= 0;
		}

		private void SaveProfile()
		{
			var serializer = new JsonSerializer();
			using (var sw = new StreamWriter(_profilePath))
			{
				using (var jtw = new JsonTextWriter(sw))
				{
					serializer.Serialize(jtw, _profiles);
				}
			}
			NewLog("Profiles updated", LogType.Blue);
		}

		private void BtnDisableTime(object sender, int time = 2000)
		{
			((Button)sender).IsEnabled = false;
			Task.Delay(time).ContinueWith(t => {
				Dispatcher.Invoke(() => {
					((Button)sender).IsEnabled = true;
				});
			});
		}

		private static readonly Dictionary<RoutinePattern, string> ComboboxRoutine = new Dictionary<RoutinePattern, string> {
			{ RoutinePattern.RunOnce, "Run Once" },
			{ RoutinePattern.RefreshAndRepeat, "Refresh and Repeat" },
			{ RoutinePattern.UntilZeroEnergy, "Until Zero Energy" }
		};
		public Dictionary<RoutinePattern, string> EnumComboboxRoutine => ComboboxRoutine;

		private static readonly Dictionary<ElementalType, string> ComboboxElementalType = new Dictionary<ElementalType, string>() {
			{ ElementalType.Light, "Light" },
			{ ElementalType.Dark, "Dark" },
			{ ElementalType.Fire, "Fire" },
			{ ElementalType.Water, "Water" },
			{ ElementalType.Wind, "Wind" }
		};
		public Dictionary<ElementalType, string> EnumComboboxElementalType => ComboboxElementalType;

		private static readonly List<int> ComboboxDeck = new List<int>
		{
			1, 2, 3, 4
		};

		public List<int> ComboboxDeckList => ComboboxDeck;

		private void RefreshDevicesList(object sender, RoutedEventArgs e)
		{
			if (SelectedDevice == null)
			{
				SelectedDeviceName = "";
				SelectedDeviceSerial = "";
			}
			var newlist = new ObservableCollection<string>();
			foreach (var device in Functions.GetDevices())
			{
				newlist.Add(device.SerialNumber);
			}
			if (!newlist.Contains(SelectedDevice)) SelectedDevice = null;
			DevicesList = newlist;
			NewLog($"Devices list refreshed: {DevicesList.Count} Device{(DevicesList.Count > 1 ? "s" : "")}");
		}

		public void SetBotStatus(string txt, Brush color = null)
		{
			if (color == null) color = new SolidColorBrush(Colors.Black);
			BotStatus = txt;
			BotStatusColor = color;

		}

		public void NewLog(string txt, LogType type = LogType.Black, bool debug = false)
		{
			SolidColorBrush color;
			switch (type)
			{
				case LogType.Black:
					color = Brushes.Black;
					break;
				case LogType.Green:
					color = Brushes.ForestGreen;
					break;
				case LogType.Red:
					color = Brushes.Brown;
					break;
				case LogType.Blue:
					color = Brushes.DeepSkyBlue;
					break;
				default:
					color = Brushes.Black;
					break;
			}
			Dispatcher.Invoke(() => {
				var time = DateTime.Now;
				var dTxt = debug ? "[DEBUG]" : "";
				TbLog.Inlines.Add(new Run($"[{time.ToString("HH:mm:ss", CultureInfo.InvariantCulture)}]{dTxt}: {txt}.{Environment.NewLine}") { Foreground = color });
				LogScroll.ScrollToEnd();
			});
		}

		public Brush BotStatusColor {
			get => _botStatusColor;
			set {
				if (Equals(value, _botStatusColor)) return;
				_botStatusColor = value;
				OnPropertyChanged();
			}
		}

		public string BotStatus {
			get => _botStatus;
			set {
				if (value == _botStatus) return;
				_botStatus = value;
				OnPropertyChanged();
			}
		}

		public SessionLog LogSession {
			get => _logSession;
			set {
				if (value == _logSession) return;
				_logSession = value;
				OnPropertyChanged();
			}
		}

		public bool IsFieldsEnabled {
			get => _isFieldsEnabled;
			set {
				if (value == _isFieldsEnabled) return;
				_isFieldsEnabled = value;
				OnPropertyChanged();
			}
		}

		public JsonConfig DataBindingConfig {
			get => _dataBindingConfig;
			set {
				if (Equals(value, _dataBindingConfig)) return;
				_dataBindingConfig = value;
				OnPropertyChanged();
			}
		}

		public string SelectedDeviceSerial {
			get => _selectedDeviceSerial;
			set {
				if (value == _selectedDeviceSerial) return;
				_selectedDeviceSerial = !string.IsNullOrEmpty(value) ? value : "<none>";
				OnPropertyChanged();
			}
		}

		public string SelectedDeviceName {
			get => _selectedDeviceName;
			set {
				if (value == _selectedDeviceName) return;
				_selectedDeviceName = !string.IsNullOrEmpty(value) ? value : string.Empty;
				OnPropertyChanged();
			}
		}

		public ObservableCollection<string> DevicesList {
			get => _devicesList;
			set {
				if (value == _devicesList) return;
				_devicesList = value;
				OnPropertyChanged();
			}
		}

		public string SelectedDevice {
			get => _selectedDevice;
			set {
				if (value == _selectedDevice || string.IsNullOrEmpty(value)) return;
				_selectedDevice = value;

				SelectedDeviceSerial = value;
				DataBindingConfig = ReturnProfileFromSerial(value);
				SelectedDeviceName = DataBindingConfig.WizardName;
				IsFieldsEnabled = true;

				NewLog($"Selected device: {value}");
				OnPropertyChanged();
			}
		}

		public Wizard LogWizard {
			get => _logWizard;
			set {
				if (value == _logWizard) return;
				_logWizard = value;
				OnPropertyChanged();
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;

		protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		private void SS_Click(object sender, RoutedEventArgs e)
		{
			var dev = AdbHelper.Instance.GetDevices(AndroidDebugBridge.SocketAddress)
				.Where(d => d.State == DeviceState.Online)
				.ToList()
				.First();
			dev.Screenshot.ToImage().Save("D:/SW Project/SW-Pic/" + DateTime.Now.Ticks + ".png");
		}
	}

	public enum LogType
	{
		Black,
		Green,
		Red,
		Blue,
	}

	public enum Pattern
	{
		RunOnce = 1,
		OnlyRepeat = 2,
		RefreshAndRepeat = 3,
		ZeroEnergy = 4
	}
}
