using PalmSens;
using PalmSens.Comm;
using PalmSens.Core.Simplified.Data;
using PalmSens.Core.Simplified.XF.Application.Services;
using PSExampleApp.Common.Models;
using PSExampleApp.Core.Extentions;
using PSExampleApp.Core.Services;
using PSExampleApp.Forms.Navigation;
using PSExampleApp.Forms.Resx;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.CommunityToolkit.ObjectModel;
using Xamarin.Essentials;

namespace PSExampleApp.Forms.ViewModels
{
    public class RunMeasurementViewModel : BaseAppViewModel
    {
        private readonly IDeviceService _deviceService;
        private readonly IMeasurementService _measurementService;
        private readonly IMessageService _messageService;
        private SimpleCurve _activeCurve;
        private Countdown _countdown = new Countdown();

        private bool _measurementFinished = false;
        private double _progress;
        private int _progressPercentage;

        public ObservableCollection<RawDataPoint> RawDataTable { get; } = new ObservableCollection<RawDataPoint>();


        public RunMeasurementViewModel(IMeasurementService measurementService, IMessageService messageService, IDeviceService deviceService, IAppConfigurationService appConfigurationService) : base(appConfigurationService)
        {
            Progress = 0;
            _deviceService = deviceService;
            _messageService = messageService;
            _measurementService = measurementService;
            _measurementService.DataReceived += _measurementService_DataReceived;
            _measurementService.MeasurementEnded += _measurementService_MeasurementEnded;

            ActiveMeasurement = _measurementService.ActiveMeasurement;

            OnPageAppearingCommand = CommandFactory.Create(OnPageAppearing, onException: ex =>
                            MainThread.BeginInvokeOnMainThread(() =>
                            {
                                //DisplayAlert();
                                Console.WriteLine(ex.Message);
                            }), allowsMultipleExecutions: false);

            ContinueCommand = CommandFactory.Create(Continue);
        }

        //public HeavyMetalMeasurement ActiveMeasurement { get; }
        public HeavyMetalMeasurement ActiveMeasurement { get; set; }


        public ICommand ContinueCommand { get; }

        public bool MeasurementIsFinished
        {
            get => _measurementFinished;
            set => SetProperty(ref _measurementFinished, value);
        }

        public ICommand OnPageAppearingCommand { get; }

        public double Progress
        {
            get => _progress;
            set => SetProperty(ref _progress, value);
        }

        public int ProgressPercentage
        {
            get => _progressPercentage;
            set => SetProperty(ref _progressPercentage, value);
        }

        public ObservableCollection<string> ReceivedData { get; set; } = new ObservableCollection<string>();

        private void _measurementService_DataReceived(object sender, SimpleCurve activeSimpleCurve)
        {
            _activeCurve = activeSimpleCurve;
            activeSimpleCurve.NewDataAdded += ActiveSimpleCurve_NewDataAdded;
        }

        private void _measurementService_MeasurementEnded(object sender, EventArgs e)
        {
            _activeCurve.NewDataAdded -= ActiveSimpleCurve_NewDataAdded;
            _countdown.Ticked -= OnCountdownTicked;

            RunPeakAnalysis().WithCallback(
                onError: async (ex) =>
                {
                    _messageService.LongAlert(AppResources.Alert_SomethingWrong);
                    Debug.WriteLine(ex);
                    _measurementService.ResetMeasurement();
                    await _deviceService.DisconnectDevice();
                    await NavigationDispatcher.PopToRoot();
                });
        }

        private void ActiveSimpleCurve_NewDataAdded(object sender, PalmSens.Data.ArrayDataAddedEventArgs e)
        {
            int startIndex = e.StartIndex; //The index of the first new data point added to the curve
            int count = e.Count; //The number of new data points added to the curve

            for (int i = startIndex; i < startIndex + count; i++)
            {
                double xValue = _activeCurve.XAxisValue(i); //Get the value on Curve's X-Axis (potential) at the specified index
                double yValue = _activeCurve.YAxisValue(i); //Get the value on Curve's Y-Axis (current) at the specified index

                RawDataTable.Add(new RawDataPoint
                {
                    Potential = xValue,
                    Current = yValue
                });


                Debug.WriteLine($"Data received potential {xValue}, current {yValue}");
                ReceivedData.Add($"potential {xValue}, current {yValue}");
            }
        }

        private async Task Continue()
        {
            //The continue will trigger the save of the measurement. //TODO maybe add cancel in case user doesn't want to save
            ActiveMeasurement.MeasurementDate = DateTime.Now.Date;
            await _measurementService.SaveMeasurement(ActiveMeasurement);
            await NavigationDispatcher.Push(NavigationViewType.MeasurementFinishedView);
        }

        private void Curve_DetectedPeaks(object sender, EventArgs e)
        {
            _measurementService.CalculateConcentration();

            //After the concentration is calculated we allow the user to press continue
            Progress = 1;
            ProgressPercentage = 100;
            MeasurementIsFinished = true;
        }

        private async Task<Method> LoadMethod()
        {
            Method method = null;

            try
            {
                method = await _appConfigurationService.LoadConfigurationMethod();
            }
            catch (Exception)
            {
                // When the method file cannot be found it means that it's manually removed. In this case the app needs to be reinstalled
                MainThread.BeginInvokeOnMainThread(() => _messageService.ShortAlert(AppResources.Alert_MethodNotFound));
                throw;
            }

            var errors = method.Validate(_measurementService.Capabilities);

            if (errors.Any(error => error.IsFatal))
            {
                MainThread.BeginInvokeOnMainThread(() => _messageService.ShortAlert(AppResources.Alert_MethodIncompatible));
                throw new Exception("The method is not compatible with connected device.\n\n" + string.Join('\n', errors.Where(error => error.IsFatal).Select(error => error.Message)));
            }

            return method;
        }

        private void OnCountdownTicked()
        {
            Progress = _countdown.ElapsedTime / _countdown.TotalTimeInMilliSeconds;
            ProgressPercentage = (int)(Progress * 100);
        }

        //private async Task OnPageAppearing()
        //{
        //    try
        //    {
        //        //var method = await LoadMethod();
        //        var method = await LoadDiffPulseMethod();

        //        _countdown.Start((int)Math.Round(method.GetMinimumEstimatedMeasurementDuration(_measurementService.Capabilities) * 1000));
        //        _countdown.Ticked += OnCountdownTicked;

        //        _measurementService.ActiveMeasurement.Measurement = await _measurementService.StartMeasurement(method);
        //    }
        //    catch (NullReferenceException)
        //    {
        //        // Nullreference is thrown when device is not connected anymore. In this case we pop back to homescreen. The user can then try to reconnect again
        //        _messageService.ShortAlert(AppResources.Alert_NotConnected);
        //        this._measurementService.ResetMeasurement();
        //        try { await _deviceService.DisconnectDevice(); }
        //        finally { await NavigationDispatcher.PopToRoot(); }
        //    }
        //    catch (ArgumentException)
        //    {
        //        // Argument exception is thrown when method is incompatible with the connected device.
        //        _messageService.ShortAlert(AppResources.Alert_DeviceIncompatible);
        //        this._measurementService.ResetMeasurement();
        //        try { await _deviceService.DisconnectDevice(); }
        //        finally { await NavigationDispatcher.PopToRoot(); }
        //    }
        //    catch (Exception ex)
        //    {
        //        _messageService.LongAlert(AppResources.Alert_SomethingWrong);
        //        Debug.WriteLine(ex);
        //        this._measurementService.ResetMeasurement();
        //        try { await _deviceService.DisconnectDevice(); }
        //        finally { await NavigationDispatcher.PopToRoot(); }
        //    }
        //}


        private async Task<bool> RunPeakAnalysis()
        {

            try
            {
                _activeCurve.DetectedPeaks += Curve_DetectedPeaks;

                var peakType = PeakTypes.Default;
                if (ActiveMeasurement.Measurement.MeasurementType == MeasurementTypes.LinearSweepVoltammetry ||
                    ActiveMeasurement.Measurement.MeasurementType == MeasurementTypes.CyclicVoltammetry)
                {
                    peakType = PeakTypes.LSVCV;
                }

                await _activeCurve.DetectPeaksAsync(
                    ActiveMeasurement.Configuration.ConcentrationMethod.PeakMinWidth,
                    ActiveMeasurement.Configuration.ConcentrationMethod.PeakMinHeight,
                    true,
                    peakType);
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Peak analysis failed: {ex}");
                return false;
            }


        }

        private async Task<Method> LoadDiffPulseMethod()
        {
            try
            {
                return await _appConfigurationService.LoadConfigurationMethod();
            }
            catch (Exception)
            {
                // When the method file cannot be found it means that it's manually removed. In this case the app needs to be reinstalled
                MainThread.BeginInvokeOnMainThread(() => _messageService.ShortAlert(AppResources.Alert_MethodNotFound));
                throw;
            }
        }

        private async Task RunRepeatMeasurement(int repeatCount, int delayInSeconds)
        {
            for (int i = 0; i < repeatCount; i++)
            {
                try
                {
                    Debug.WriteLine($"[Repeat] Starting measurement {i + 1} / {repeatCount}");

                    // Reset progress and raw data
                    Progress = 0;
                    ProgressPercentage = 0;
                    MeasurementIsFinished = false;
                    RawDataTable.Clear();

                    var method = await LoadDiffPulseMethod();

                    _countdown.Start((int)Math.Round(method.GetMinimumEstimatedMeasurementDuration(_measurementService.Capabilities) * 1000));
                    _countdown.Ticked += OnCountdownTicked;

                    var newMeasurement = _measurementService.CreateMeasurement(_measurementService.ActiveMeasurement.Configuration);
                    newMeasurement.Name = $"Sample_{DateTime.Now:yyyyMMdd_HHmmss}";
                    ActiveMeasurement = newMeasurement;

                    var simpleMeasurement = await _measurementService.StartMeasurement(method);
                    ActiveMeasurement.Measurement = simpleMeasurement;
                    ActiveMeasurement.MeasurementDate = DateTime.Now;

                    var measurementFinished = new TaskCompletionSource<bool>();
                    void OnEnd(object sender, EventArgs e)
                    {
                        _measurementService.MeasurementEnded -= OnEnd;
                        measurementFinished.TrySetResult(true);
                    }

                    _measurementService.MeasurementEnded += OnEnd;
                    await measurementFinished.Task;

                    var success = await RunPeakAnalysis();
                    if (!success)
                        Debug.WriteLine($"[Repeat] Skipped peak analysis for measurement {i + 1}");

                    await _measurementService.SaveMeasurement(ActiveMeasurement);
                    await SaveRawDataAsCsvAsync(ActiveMeasurement.Name, RawDataTable);

                    Debug.WriteLine($"[Repeat] Measurement {i + 1} saved.");

                    if (i < repeatCount - 1)
                    {
                        Debug.WriteLine($"[Repeat] Waiting {delayInSeconds} seconds before next run...");
                        await Task.Delay(delayInSeconds * 1000);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[Repeat] Error during measurement {i + 1}: {ex.Message}");
                }
            }

            Debug.WriteLine("[Repeat] All measurements completed.");
        }




        private async Task OnPageAppearing()
        {
            try
            {
                await RunRepeatMeasurement(3, 20);
            }
            catch (Exception ex)
            {
                _messageService.LongAlert(AppResources.Alert_SomethingWrong);
                Debug.WriteLine(ex);
                _measurementService.ResetMeasurement();
                try { await _deviceService.DisconnectDevice(); }
                finally { await NavigationDispatcher.PopToRoot(); }
            }
        }

        private async Task SaveRawDataAsCsvAsync(string sampleName, ObservableCollection<RawDataPoint> rawData)
        {
            try
            {
                string fileName = $"{sampleName}.csv";
                string header = "Potential (V),Current (A)";

#if ANDROID
string folder = Path.Combine(Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDownloads).AbsolutePath,"PSData");
#elif IOS
var folder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
#else
                var folder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
#endif


                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                string fullPath = Path.Combine(folder, fileName);

                using (var writer = new StreamWriter(fullPath, false))
                {
                    await writer.WriteLineAsync(header);
                    foreach (var point in rawData)
                        await writer.WriteLineAsync($"{point.Potential},{point.Current}");
                }

                Debug.WriteLine($"[CSV] Saved raw data to: {fullPath}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CSV] Failed to save raw data: {ex.Message}");
            }
        }



    }
}