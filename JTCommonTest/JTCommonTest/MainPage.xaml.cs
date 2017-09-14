

namespace JTCommonTest
{
    using JTCommonTest.Interfaces;
    using JTCommonTest.ViewModel;
    using Plugin.BluetoothLE;
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Xamarin.Forms;

    public partial class MainPage : ContentPage
    {
        ObservableCollection<BlueTooth> bluetooh = new ObservableCollection<BlueTooth>();
        IBluetooth BluetoothService;
        public MainPage()
        {
            InitializeComponent();
            bluetooh.Add(new BlueTooth { DisplayName = "Test 1" });
            BlueToothView.ItemsSource = bluetooh;

            BluetoothService = DependencyService.Get<IBluetooth>();
            BluetoothService.Scan(5000);


            //var adapterStatus = CrossBleAdapter.Current.Status;

            //var tt = CrossBleAdapter.Current.WhenStatusChanged().Subscribe(status =>
            //{
            //    System.Diagnostics.Debug.WriteLine("Device " + status.ToString());
            //    if (status == AdapterStatus.PoweredOn)
            //    {
            //        System.Diagnostics.Debug.WriteLine("Device Enter " + status.ToString());
            //        var scanner = CrossBleAdapter.Current.Scan().Subscribe(scanResult =>
            //        {
            //            // do something with it
            //            // the scanresult contains the device, RSSI, and advertisement packet
            //            if (!string.IsNullOrEmpty(scanResult.Device.ToString()))
            //            {
            //                scanResult.Device.Connect();

            //                if (!bluetooh.Any(s => s.DisplayName.Equals(scanResult.Device.ToString())))
            //                {
            //                    System.Diagnostics.Debug.WriteLine("Device " + scanResult.Device.Name);
            //                    bluetooh.Add(new BlueTooth { DisplayName = scanResult.Device.ToString() });
            //                }
            //            }

            //        });

            //    }
            //});

            //var scanner = CrossBleAdapter.Current.Scan().Subscribe(scanResult =>
            //{
            //    // do something with it
            //    // the scanresult contains the device, RSSI, and advertisement packet
            //    if(!string.IsNullOrEmpty(scanResult.Device.Name))
            //    {
            //        if (!bluetooh.Any(s => s.DisplayName.Equals(scanResult.Device.Name)))
            //        {
            //            System.Diagnostics.Debug.WriteLine("Device " + scanResult.Device.Name);
            //            bluetooh.Add(new BlueTooth { DisplayName = scanResult.Device.Name });
            //        }                    
            //    }                
            //});
        }

        public void OnButtonClicked(object sender, EventArgs args)
        {

            //((Button)sender).Text =
            //    String.Format("{0} click{1}!", count, count == 1 ? "" : "s");

            
            BluetoothService.Scan(5000);
        }

    }
}
