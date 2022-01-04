using nanoFramework.Device.Bluetooth.Spp;
using nanoFramework.Hardware.Esp32;
using nanoFramework.Networking;
using nanoFramework.Runtime.Native;
using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using Windows.Devices.WiFi;
using Memory = nanoFramework.Runtime.Native.GC;
namespace nanoframework_nordicspp
{
    public class Program
    {
        static NordicSpp spp;
        static WiFiAdapter wifi;

        public static void Main()
        {
            PrintMemory("Start");
            Debug.WriteLine("\nSerial Terminal over Bluetooth LE Sample");
            
            // Create Instance of Bluetooth Serial profile
            spp = new NordicSpp();

            PrintMemory("After NordicSPP");

            // Add event handles for received data and Connections 
            spp.ReceivedData += Spp_ReceivedData;
            spp.ConnectedEvent += Spp_ConnectedEvent;

            // Start Advertising SPP service
            spp.Start("nanoFrameworkSerial");
            int count = 0;
            uint totalSize, totalFreeSize, largestBlock;

            while (true)
            {
                count++;
                Thread.Sleep(10_000);
                if (spp.IsConnected && !spp.SendString($"Current device time:{DateTime.UtcNow} Count:{count}\n"))
                {
                    Debug.WriteLine($"Send failed!");
                }
                if(NetworkHelper.IsValidIpAddress(System.Net.NetworkInformation.NetworkInterfaceType.Wireless80211))
                {
                    PrintMemory("MQTT");
                    NativeMemory.GetMemoryInfo(NativeMemory.MemoryType.Internal, out totalSize, out totalFreeSize, out largestBlock);
                    MQTT.Publish(Encoding.UTF8.GetBytes($"Count:{count} Internal Mem: {totalFreeSize} Largest Mem: {largestBlock}"));
                    //Debug.WriteLine("Sending MQTT");
                }
            }
        }


        private static void Spp_ConnectedEvent(IBluetoothSpp sender, EventArgs e)
        {
            if (spp.IsConnected)
            {
                spp.SendString($"Welcome to Bluetooth Serial sample\n");
                spp.SendString($"Send 'help' for options\n");
            }

            Debug.WriteLine($"Client connected:{sender.IsConnected}");
        }

        private static void Spp_ReceivedData(IBluetoothSpp sender, SppReceivedDataEventArgs ReadRequestEventArgs)
        {
            string message = ReadRequestEventArgs.DataString;
            Debug.WriteLine($"Received=>{message}");

            string[] args = message.Trim().Split(' ');
            if (args.Length != 0)
            {
                uint totalSize, totalFreeSize, largestBlock;

                switch (args[0].ToLower())
                {
                    // Scan for wifi networks
                    case "scan":
                        InitWiFiScan();
                        sender.SendString("Scanning Networks\n");
                        wifi.ScanAsync();
                        break;

                    // set WiFi credentials "wifi mywifi password123"
                    case "wifi":
                        if (args.Length != 3) // arg[1] is wifi ssid,  arg[2] is password
                        {
                            sender.SendString("Wrong number of arguments\n");
                            break;
                        }
                        CancellationTokenSource cancelToken = new CancellationTokenSource(60_000);
                        bool isConnected = NetworkHelper.ConnectWifiDhcp(args[1], args[2], WiFiReconnectionKind.Automatic, setDateTime: true, token: cancelToken.Token);
                        PrintMemory("wifi");
                        if (isConnected)
                            sender.SendString("Set Wifi credential Successful\n");
                        else
                            sender.SendString("Set Wifi credential Failed\n");

                        // Save credentials Here

                        break;

                    // Send current ESP32 native memory
                    case "mem":
                       
                        NativeMemory.GetMemoryInfo(NativeMemory.MemoryType.Internal, out totalSize, out totalFreeSize, out largestBlock);
                        sender.SendString($"Native memory - total:{totalSize} Free:{totalFreeSize} largest:{largestBlock}\n");
                        break;

                    // Reboot device
                    case "reboot":
                        sender.SendString("Rebooting now\n");
                        Thread.Sleep(100);
                        Power.RebootDevice();
                        break;

                    // Some help
                    case "help":
                        sender.SendString("Help\n");
                        sender.SendString("-------------------------------------------\n");
                        sender.SendString("'scan' - Scan WiFi networks\n");
                        sender.SendString("'mem' - Show native free memory\n");
                        sender.SendString("'reboot' - Reboot device\n");
                        sender.SendString("'wifi ssid password' - Set WiFI credentials\n");
                        sender.SendString("-------------------------------------------\n");
                        break;

                }
            }
        }


        private static void PrintMemory(string msg)
        {
            uint totalSize, totalFreeSize, largestBlock;
            NativeMemory.GetMemoryInfo(NativeMemory.MemoryType.Internal, out totalSize, out totalFreeSize, out largestBlock);
            Debug.WriteLine($"\n{msg}-> Internal Total Mem {totalSize} Total Free {totalFreeSize} Largest Block {largestBlock}");
            Debug.WriteLine($"nF Mem {Memory.Run(false)}\n");
        }

        #region WiFi Scanning
        private static void InitWiFiScan()
        {
            if (wifi == null)
            {
                // Get the first WiFI Adapter
                wifi = WiFiAdapter.FindAllAdapters()[0];

                // Set up the AvailableNetworksChanged event to pick up when scan has completed
                wifi.AvailableNetworksChanged += Wifi_AvailableNetworksChanged;
            }
        }

        private static void Wifi_AvailableNetworksChanged(WiFiAdapter sender, object e)
        {
            if (spp.IsConnected)
            {
                // Get Report of all scanned WiFi networks
                WiFiNetworkReport report = sender.NetworkReport;

                // Enumerate though networks and send to client
                foreach (WiFiAvailableNetwork net in report.AvailableNetworks)
                {
                    // Show all networks found
                    if (spp.IsConnected)
                    {
                        spp.SendString($"Net SSID :{net.Ssid},  BSSID : {net.Bsid},  rssi : {net.NetworkRssiInDecibelMilliwatts.ToString()},  signal : {net.SignalBars.ToString()}\n");
                    }
                }
            }
        }
        #endregion
        
    }
}
