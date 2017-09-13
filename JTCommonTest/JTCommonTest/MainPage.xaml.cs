using JTCommonTest.ViewModel;
using Plugin.BluetoothLE;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace JTCommonTest
{
    public partial class MainPage : ContentPage
    {
        ObservableCollection<BlueTooth> bluetooh = new ObservableCollection<BlueTooth>();

        public MainPage()
        {
            InitializeComponent();
            bluetooh.Add(new BlueTooth { DisplayName = "Test 1" });
            BlueToothView.ItemsSource = bluetooh;
        
            var scanner = CrossBleAdapter.Current.Scan().Subscribe(scanResult =>
            {
                // do something with it
                // the scanresult contains the device, RSSI, and advertisement packet
                if(!string.IsNullOrEmpty(scanResult.Device.Name))
                {
                    if (!bluetooh.Any(s => s.DisplayName.Equals(scanResult.Device.Name)))
                    {
                        System.Diagnostics.Debug.WriteLine("Device " + scanResult.Device.Name);
                        bluetooh.Add(new BlueTooth { DisplayName = scanResult.Device.Name });
                    }                    
                }
                

            });

        }
    }
}
