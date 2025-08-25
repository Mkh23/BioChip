﻿using PalmSens.Devices;
using PalmSens.Core.Simplified.XF.Application.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PalmSens.Core.Simplified.XF.Application.Services
{
    public class InstrumentService : PSCommSimple
    {
        private readonly IInstrumentPlatfrom _platformDeviceManager;

        public InstrumentService(IInstrumentPlatfrom platform) : base(platform)
        {
            _platformDeviceManager = platform;
        }

        public event EventHandler<PlatformDevice> DeviceDiscovered
        {
            add => _platformDeviceManager.DeviceDiscovered += value;
            remove => _platformDeviceManager.DeviceDiscovered -= value;
        }

        public event EventHandler<PlatformDevice> DeviceRemoved
        {
            add => _platformDeviceManager.DeviceRemoved += value;
            remove => _platformDeviceManager.DeviceRemoved -= value;
        }

        public Task<List<PlatformDevice>> GetConnectedDevices(CancellationToken? cancellationToken = null)
        {
            return _platformDeviceManager.GetConnectedDevices(cancellationToken);
        }
        
        public void InitializeInstrument(Method method)
        {
            if (this.Comm.Capabilities is EmStatPicoCapabilities picoCapabilities)
            {
                method.DeterminePGStatMode(picoCapabilities);
                picoCapabilities.ActiveSignalTrainConfiguration = method.PGStatMode;
                method.Ranging.SupportedCurrentRanges = Capabilities.SupportedRanges;
            }
        }
    }
}