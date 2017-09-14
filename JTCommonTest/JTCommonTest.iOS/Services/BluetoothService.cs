
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

    public class BluetoothService :  IBluetooth ,IDisposable
    {
        private const int ConnectionTimeout = 10000;
        private readonly CBCentralManager manager = new CBCentralManager();
        private string DeviceName = "Printer";
        public int ScanTime = 5000;
        string GATTServices = "180A"; //https://www.bluetooth.com/specifications/gatt/services

        //string GATTServices = "180A";
        //public EventHandler<CBPeripheral> DiscoveredDevice;
        //public EventHandler<CBCentralManagerState> StateChanged;

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

        public async Task Scan(int scanDuration, string serviceUuid = "")
        {
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
                //byte[] bufferToPrint = Encoding.ASCII.GetBytes("Hola");
                //NSData data = NSData.FromArray(bufferToPrint);
                //peripheral.WriteValue(data,null);

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

        public async Task<CBService> GetService(CBPeripheral peripheral, string serviceUuid)
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
                if (this.GetServiceIfDiscovered(peripheral, serviceUuid) != null)
                {
                    taskCompletion.SetResult(true);
                }
            };

            try
            {
                peripheral.DiscoveredService += handler;
                peripheral.DiscoverServices(new[] { CBUUID.FromString(serviceUuid) });
                await this.WaitForTaskWithTimeout(task, ConnectionTimeout);
                return this.GetServiceIfDiscovered(peripheral, serviceUuid);
            }
            finally
            {
                peripheral.DiscoveredService -= handler;
            }
        }

        public CBService GetServiceIfDiscovered(CBPeripheral peripheral, string serviceUuid)
        {
            serviceUuid = serviceUuid.ToLowerInvariant();
            return peripheral.Services
                ?.FirstOrDefault(x => x.UUID?.Uuid?.ToLowerInvariant() == serviceUuid);
        }

        public async Task<CBCharacteristic[]> GetCharacteristics(CBPeripheral peripheral, CBService service, int scanTime)
        {
            peripheral.DiscoverCharacteristics(service);
            await Task.Delay(scanTime);
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
                peripheral.WriteValue(value, characteristic, CBCharacteristicWriteType.WithoutResponse);
                                
                await this.WaitForTaskWithTimeout(task, ConnectionTimeout);
                return task.Result;
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

                    var service = await this.GetService(peripheral, GATTServices);
                    if (service != null)
                    {
                        var characteristics = await this.GetCharacteristics(peripheral, service, ScanTime);
                        foreach (var characteristic in characteristics)
                        {
                            Debug.WriteLine($" Find characteristic {characteristic.UUID.Description}");
                            //var value = await this.ReadValue(peripheral, characteristic);
                            //byte[] bufferToPrint = Encoding.ASCII.GetBytes("Hola");
                            NSData data = NSData.FromString("A0");
                            
                            //NSData data = NSData.FromArray(bufferToPrint);
                            //peripheral.WriteValue(data,null);


                            await this.WriteValue(peripheral, characteristic, data);
                            //Debug.WriteLine($"{characteristic.UUID.Description} = {value}");
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
            await Task.WhenAny(task, Task.Delay(ConnectionTimeout));
            if (!task.IsCompleted)
            {
                throw new TimeoutException();
            }
        }
    }
}
