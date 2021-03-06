﻿
using Xamarin.Forms;

[assembly: Dependency(typeof(JTCommonTest.iOS.Services.BluetoothService))]
namespace JTCommonTest.iOS.Services
{
    using System;
    using CoreBluetooth;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using System.Linq;
    using Foundation;
    using JTCommonTest.Interfaces;
    using System.Text;
    using System.Collections.Generic;

    public class BluetoothService :  IBluetooth ,IDisposable
    {
        private const int ConnectionTimeout = 1000;
        private readonly CBCentralManager manager = new CBCentralManager();
        private string DeviceName = "Printer";
        private int ScanTime = 500;
        string GATTServices = "180A"; //https://www.bluetooth.com/specifications/gatt/services        

        public BluetoothService()
        {
            this.manager.DiscoveredPeripheral += this.DiscoveredPeripheral;
            this.manager.UpdatedState += this.UpdatedState;
            //this.Scan(ScanTime);
        }

        public void Dispose()
        {
            this.manager.DiscoveredPeripheral -= this.DiscoveredPeripheral;
            this.manager.UpdatedState -= this.UpdatedState;
            this.StopScan();
        }

        public async Task Print(List<string> hashd, int scanDuration)
        {
            await this.Scan(scanDuration);
        }

        public async Task Scan(int scanDuration, string serviceUuid = "")
        {
            ScanTime = scanDuration;
            Debug.WriteLine("Scanning started");
            var uuids = string.IsNullOrEmpty(serviceUuid)
                ? new CBUUID[0]
                : new[] { CBUUID.FromString(serviceUuid) };
            this.manager.ScanForPeripherals(uuids);

            await Task.Delay(scanDuration);
            this.StopScan();
        }

        public void StopScan()
        {
            this.manager.StopScan();
            Debug.WriteLine("Scanning stopped");
        }

        public async Task ConnectTo(CBPeripheral peripheral)
        {
            var taskCompletion = new TaskCompletionSource<bool>();
            var task = taskCompletion.Task;
            EventHandler<CBPeripheralEventArgs> connectedHandler = (s, e) =>
            {
                if (e.Peripheral.Identifier?.ToString() == peripheral.Identifier?.ToString())
                {
                    taskCompletion.SetResult(true);
                }
            };

            try
            {
                this.manager.ConnectedPeripheral += connectedHandler;
                this.manager.ConnectPeripheral(peripheral);
                await this.WaitForTaskWithTimeout(task, ConnectionTimeout);
                Debug.WriteLine($"Bluetooth device connected = {peripheral.Name}");
            }
            finally
            {
                this.manager.ConnectedPeripheral -= connectedHandler;
            }
        }

        public void Disconnect(CBPeripheral peripheral)
        {
            this.manager.CancelPeripheralConnection(peripheral);
            Debug.WriteLine($"Device {peripheral.Name} disconnected");
        }

        public CBPeripheral[] GetConnectedDevices(string serviceUuid)
        {
            return this.manager.RetrieveConnectedPeripherals(new[] { CBUUID.FromString(serviceUuid) });
        }

        public async Task<IEnumerable<CBService>> GetService(CBPeripheral peripheral, string serviceUuid)
        {
            var service = this.GetServiceIfDiscovered(peripheral, serviceUuid);
            if (service != null)
            {
                return service;
            }

            var taskCompletion = new TaskCompletionSource<bool>();
            var task = taskCompletion.Task;
            EventHandler<NSErrorEventArgs> handler = (s, e) =>
            {
                if (this.GetServiceIfDiscovered(peripheral, serviceUuid)?.Any() == true)
                {
                    taskCompletion.SetResult(true);
                }
            };  

            try
            {
                peripheral.DiscoveredService += handler;
                peripheral.DiscoverServices();
                await this.WaitForTaskWithTimeout(task, ConnectionTimeout);
                return this.GetServiceIfDiscovered(peripheral, serviceUuid);
            }
            finally
            {
                peripheral.DiscoveredService -= handler;
            }
        }

        public IEnumerable<CBService> GetServiceIfDiscovered(CBPeripheral peripheral, string serviceUuid)
        {
            serviceUuid = serviceUuid.ToLowerInvariant();
            return peripheral.Services;
            //return peripheral.Services
            //    ?.FirstOrDefault(x => x.UUID?.Uuid?.ToLowerInvariant() == serviceUuid);
        }

        public IEnumerable<CBCharacteristic> GetCharacteristicIfDiscovered(CBService service, string serviceUuid)
        {
            serviceUuid = serviceUuid.ToLowerInvariant();
            return service.Characteristics;            
        }

        public async Task<IEnumerable<CBCharacteristic>> GetCharacteristics(CBPeripheral peripheral, CBService service, int scanTime)
        {
            //peripheral.DiscoverCharacteristics(service);
            //peripheral.DiscoveredCharacteristic
            //await Task.Delay(scanTime);

            var taskCompletion = new TaskCompletionSource<bool>();
            var task = taskCompletion.Task;

            peripheral.DiscoverCharacteristics(service);

            EventHandler<CBServiceEventArgs> handler =  (s, e)  =>
            {                    
                if (GetCharacteristicIfDiscovered(e.Service,"").Any())
                {
                    taskCompletion.SetResult(true);
                }                
            };
            try
            {
                peripheral.DiscoveredCharacteristic += handler;
                await this.WaitForTaskWithTimeout(task, ConnectionTimeout);
            }
            finally
            {
                peripheral.DiscoveredCharacteristic -= handler;
            }

            return service.Characteristics;
        }

        public async Task<NSData> ReadValue(CBPeripheral peripheral, CBCharacteristic characteristic)
        {
            var taskCompletion = new TaskCompletionSource<bool>();
            var task = taskCompletion.Task;
            EventHandler<CBCharacteristicEventArgs> handler = (s, e) =>
            {
                if (e.Characteristic.UUID?.Uuid == characteristic.UUID?.Uuid)
                {
                    taskCompletion.SetResult(true);
                }
            };

            try
            {
                peripheral.UpdatedCharacterteristicValue += handler;
                peripheral.ReadValue(characteristic);
                await this.WaitForTaskWithTimeout(task, ConnectionTimeout);
                return characteristic.Value;
            }
            finally
            {
                peripheral.UpdatedCharacterteristicValue -= handler;
            }
        }

        public async Task<NSError> WriteValue(CBPeripheral peripheral, CBCharacteristic characteristic, NSData value)
        {
            
            var taskCompletion = new TaskCompletionSource<NSError>();
            var task = taskCompletion.Task;
            EventHandler<CBCharacteristicEventArgs> handler = (s, e) =>
            {
                if (e.Characteristic.UUID?.Uuid == characteristic.UUID?.Uuid)
                {
                    taskCompletion.SetResult(e.Error);
                }
            };

            try
            {
                peripheral.WroteCharacteristicValue += handler;                
                peripheral.WriteValue(value, characteristic, CBCharacteristicWriteType.WithResponse);
                peripheral.WriteValue(NSData.FromArray(new byte[] { 10 }), characteristic, CBCharacteristicWriteType.WithoutResponse);                
                await this.WaitForTaskWithTimeout(task, ConnectionTimeout);
              
                return await task;
            }
            finally
            {
                peripheral.WroteCharacteristicValue -= handler;
            }
        }

        private async void StateChanged(object sender, CBCentralManagerState state)
        {
            if ( state == CBCentralManagerState.PoweredOn)
            {
                try
                {                    
                    var connectedDevice = this.GetConnectedDevices(GATTServices)
                        ?.FirstOrDefault(x => x.Name?.Contains(DeviceName) == true);

                    if (connectedDevice != null)
                    {
                        this.DiscoveredDevice(this, connectedDevice);
                    }
                    else
                    {
                        await this.Scan(ScanTime);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                }
            }
        }

        private async void DiscoveredDevice(object sender, CBPeripheral peripheral)
        {
            if (peripheral.Name?.Contains(DeviceName) == true)
            {
                try
                {
                    await this.ConnectTo(peripheral);

                    var taskCompletion = new TaskCompletionSource<bool>();
                    var task = taskCompletion.Task;

                    var services = await this.GetService(peripheral, GATTServices);
                    if (services != null)
                    {
                        bool continueIteration = true;
                        foreach (var service in services)
                        {
                            var characteristics = await this.GetCharacteristics(peripheral, service, ScanTime);

                            foreach (var characteristic in characteristics)
                            {
                                Debug.WriteLine($" Find characteristic {characteristic.UUID.Description}");
                                List<string> prueba = new List<string>();
                                prueba.Add(".......");
                                prueba.Add("       ");
                                prueba.Add("Hola   ");
                                prueba.Add("       ");
                                prueba.Add("Mundo   ");
                                prueba.Add("       ");
                                prueba.Add(".......");
                                NSError error = null;
                                foreach (var item in prueba)
                                {
                                    error = await WriteValue(peripheral, characteristic, NSData.FromString(item));
                                    continueIteration = !string.IsNullOrEmpty(error?.LocalizedDescription);                                 
                                }
                                if (!continueIteration)
                                {
                                    throw new InvalidOperationException(error?.LocalizedDescription);
                                }
                            }                            
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                }
                finally
                {
                    this.Disconnect(peripheral);
                }
            }
        }


        private void DiscoveredPeripheral(object sender, CBDiscoveredPeripheralEventArgs args)
        {
            var device = $"{args.Peripheral.Name} - {args.Peripheral.Identifier?.Description}";
            Debug.WriteLine($"Discovered {device}");
            this.DiscoveredDevice(sender, args.Peripheral);
        }

        private void UpdatedState(object sender, EventArgs args)
        {
            Debug.WriteLine($"State = {this.manager.State}");
            this.StateChanged(sender, this.manager.State);
        }

        private async Task WaitForTaskWithTimeout(Task task, int timeout)
        {
            //await Task.WhenAny(task, Task.Delay(ConnectionTimeout));
            await Task.WhenAny(task, Task.Delay(ConnectionTimeout));
            if (!task.IsCompleted)
            {
                throw new TimeoutException();
            }
        }
    }
}
