﻿using MvvmHelpers;
using PalmSens.Core.Simplified.XF.Application.Services;
using PSExampleApp.Common.Models;
using PSExampleApp.Core.Services;
using PSExampleApp.Forms.Navigation;
using PSExampleApp.Forms.Resx;
using Rg.Plugins.Popup.Contracts;
using Rg.Plugins.Popup.Services;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.CommunityToolkit.ObjectModel;

namespace PSExampleApp.Forms.ViewModels
{
    public class SelectMeasurementViewModel : BaseAppViewModel
    {
        private readonly IMeasurementService _measurementService;
        private readonly IMessageService _messageService;
        private readonly IPopupNavigation _popupNavigation;
        private readonly IUserService _userService;

        public SelectMeasurementViewModel(IUserService userService, IMeasurementService measurementService, IMessageService messageService, IAppConfigurationService appConfigurationService) : base(appConfigurationService)
        {
            _messageService = messageService;
            OnMeasurementSelectedCommand = CommandFactory.Create(async info => await MeasurementSelected(info as MeasurementInfo));
            DeleteCommand = CommandFactory.Create(async info => await DeleteMeasurement(info as MeasurementInfo));

            _popupNavigation = PopupNavigation.Instance;
            _userService = userService;
            _measurementService = measurementService;
            AddMeasurements();
        }

        public ObservableCollection<MeasurementInfo> AvailableMeasurements { get; } = new ObservableCollection<MeasurementInfo>();

        public ICommand DeleteCommand { get; }

        public ICommand MeasurementSelectedCommand { get; }

        public ICommand OnMeasurementSelectedCommand { get; }

        private void AddMeasurements()
        {
            if (_userService?.ActiveUser?.Measurements != null)
            {
                foreach (var measurement in _userService.ActiveUser.Measurements)
                    AvailableMeasurements.Add(measurement);
            }
        }

        private async Task DeleteMeasurement(MeasurementInfo info)
        {
            try
            {
                await _measurementService.DeleteMeasurement(info.Id);
                AvailableMeasurements.Remove(info);
            }
            catch (System.Exception)
            {
                _messageService.ShortAlert(AppResources.Alert_ErrorDeleteMeasurement);
            }
        }

        private async Task MeasurementSelected(MeasurementInfo info)
        {
            try
            {
                await _measurementService.LoadMeasurement(info.Id);
                await NavigationDispatcher.Push(NavigationViewType.MeasurementDataView);
            }
            catch (System.Exception ex)
            {
                _messageService.ShortAlert(AppResources.Alert_ErrorLoadingMeasurement);
                Debug.WriteLine(ex);
            }
        }
    }
}