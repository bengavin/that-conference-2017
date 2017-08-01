using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using ThatPiSample.Models;
using ThatPiSample.Services;
using Windows.UI;
using Windows.UI.Xaml;

namespace ThatPiSample.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        /// <summary>
        /// NOTE: Uncomment the Pokemon that are in your kit, set one as:
        /// -- Colors.Red
        /// -- Colors.Green
        /// -- Colors.Blue
        /// </summary>
        private static readonly List<Beacon> BeaconsToRecognize = new List<Beacon>
        {
            //new Beacon("Blastoise", "20160809000000000000-000000000001", Colors.Blue),
            //new Beacon("Helioptile", "20160809000000000000-000000000002", Colors.Green),
            //new Beacon("Chansey", "20160809000000000000-000000000003", Colors.Red),
            //new Beacon("Psyduck", "20160809000000000000-000000000004", Colors.Red),
            //new Beacon("Hippopotas", "20160809000000000000-000000000005", Colors.Green),
            //new Beacon("Robo Substitute", "20160809000000000000-000000000006", Colors.Blue),
            //new Beacon("Charmander", "20160809000000000000-000000000007", Colors.Blue),
            //new Beacon("Vanillite", "20160809000000000000-000000000008", Colors.Green),
            //new Beacon("Buneary", "20160809000000000000-000000000009", Colors.Red),
            //new Beacon("Flabébé", "20160809000000000000-000000000010", Colors.Orange),
        };

        public MainViewModel()
        {
            if (Windows.ApplicationModel.DesignMode.DesignModeEnabled)
            {
                BeaconsInView = "Design Mode Beacon";
            }
            else
            {
                TurnOnLedCommand = new RelayCommand<Color>(TurnOnLed);
                TurnOffLedCommand = new RelayCommand(TurnOffLed);
                PushButtonCommand = new RelayCommand(ListenForButtonPush, () => !_watchingForPush);
                StartCommand = new RelayCommand(StartBeaconListener, () => (_beaconService?.IsStarted ?? false) == false);
                StopCommand = new RelayCommand(StopBeaconListener, () => (_beaconService?.IsStarted ?? false) == true);

                // If we aren't running in debug mode, start the beacon listener
                if (!System.Diagnostics.Debugger.IsAttached)
                {
                    TurnOffLed();
                    ListenForButtonPush();
                }
            }
        }

        private const string NoBeaconsInView = "<<None>>";
        private string _beaconsInView = NoBeaconsInView;
        public string BeaconsInView
        {
            get
            {
                return _beaconsInView;
            }
            set
            {
                if (value != _beaconsInView)
                {
                    _beaconsInView = value;

                    if (String.IsNullOrWhiteSpace(_beaconsInView))
                    {
                        _beaconsInView = NoBeaconsInView;
                    }

                    RaisePropertyChanged("BeaconsInView");
                }
            }
        }

        public RelayCommand<Color> TurnOnLedCommand { get; private set; }
        public RelayCommand TurnOffLedCommand { get; private set; }
        public RelayCommand PushButtonCommand { get; private set; }
        public RelayCommand StartCommand { get; private set; }
        public RelayCommand StopCommand { get; private set; }

        private LedService _ledService;
        private void TurnOnLed(Color showColor)
        {
            if (_ledService == null)
            {
                _ledService = new LedService();
                _ledService.InitializeAsync()
                           .ContinueWith(r =>
                           {
                               if (r.IsCompleted && !r.IsFaulted)
                               {
                                   DispatcherHelper.CheckBeginInvokeOnUI(() =>
                                   {
                                       TurnOnLed(showColor);
                                   });
                               }
                           });
            }
            else
            {
                _ledService.SetLEDColor(showColor);
            }
        }

        private void TurnOffLed()
        {
            if (_ledService == null)
            {
                _ledService = new LedService();
                _ledService.InitializeAsync()
                           .ContinueWith(r =>
                           {
                               if (r.IsCompleted && !r.IsFaulted && r.Result == true)
                               {
                                   DispatcherHelper.CheckBeginInvokeOnUI(() =>
                                   {
                                       TurnOffLed();
                                   });
                               }
                           });
            }
            else
            {
                _ledService.SetLEDColor(Colors.Black);
            }
        }

        private bool _watchingForPush = false;
        private PushButtonService _buttonService;
        private void ListenForButtonPush()
        {
            if (_watchingForPush) { return; }

            _watchingForPush = true;
            if (_buttonService == null)
            {
                _buttonService = new PushButtonService();
                _buttonService.InitializeAsync()
                              .ContinueWith(r =>
                              {
                                  if (r.IsCompleted && !r.IsFaulted && r.Result == true)
                                  {
                                      _buttonService.ButtonPushed += _buttonService_ButtonPushed;
                                  }
                                  else
                                  {
                                      DispatcherHelper.CheckBeginInvokeOnUI(() =>
                                      {
                                          _watchingForPush = false;
                                          PushButtonCommand.RaiseCanExecuteChanged();
                                      });
                                  }
                              });
            }
        }

        private void _buttonService_ButtonPushed(object sender, EventArgs e)
        {
            if (_watchingForPush)
            {
                try
                {
                    _watchingForPush = false;
                    if (_beaconService == null || !_beaconService.IsStarted)
                    {
                        StartBeaconListener();
                    }
                    else
                    {
                        StopBeaconListener();
                        TurnOffLed();
                    }

                    DispatcherHelper.CheckBeginInvokeOnUI(() =>
                    {
                        System.Diagnostics.Debug.WriteLine("Got an expected button press");
                        PushButtonCommand.RaiseCanExecuteChanged();
                    });
                }
                finally
                {
                    _watchingForPush = true;
                }
            }
        }

        private BeaconService _beaconService;
        private DispatcherTimer _timer;

        private void StartBeaconListener()
        {
            if (_beaconService == null)
            {
                _beaconService = new BeaconService();
            }

            if (_timer == null)
            {
                _timer = new DispatcherTimer()
                {
                    Interval = TimeSpan.FromSeconds(0.5)
                };

                _timer.Tick += _timer_Tick;
            }

            _beaconService.Start(BeaconsToRecognize);
            StartCommand.RaiseCanExecuteChanged();
            StopCommand.RaiseCanExecuteChanged();

            _timer.Start();
        }

        private void _timer_Tick(object sender, object e)
        {
            if (!_timer.IsEnabled || _beaconService == null) { return; }

            var myBeacons = _beaconService.GetVisibleBeacons(DateTimeOffset.UtcNow.AddSeconds(-5));

            // Update LED State
            var red = myBeacons.Any() ? (byte)Math.Max(0, Math.Min(myBeacons.Sum(b => b.DisplayColor.R), 255)) : (byte)0;
            var blue = myBeacons.Any() ? (byte)Math.Max(0, Math.Min(myBeacons.Sum(b => b.DisplayColor.B), 255)) : (byte)0;
            var green = myBeacons.Any() ? (byte)Math.Max(0, Math.Min(myBeacons.Sum(b => b.DisplayColor.G), 255)) : (byte)0;

            if (_ledService != null)
            {
                _ledService.SetLEDColor(Color.FromArgb(255, red, green, blue));
            }

            // Update Display of Beacon Names
            DispatcherHelper.CheckBeginInvokeOnUI(() =>
            {
                BeaconsInView = String.Join(Environment.NewLine, myBeacons.Select(b => b.DisplayName));
            });
        }

        private void StopBeaconListener()
        {
            _timer.Stop();

            _beaconService?.Stop();
            StartCommand.RaiseCanExecuteChanged();
            StopCommand.RaiseCanExecuteChanged();
        }
    }
}
