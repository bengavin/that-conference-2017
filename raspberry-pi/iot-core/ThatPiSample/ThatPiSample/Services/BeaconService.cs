using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ThatPiSample.Models;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Storage.Streams;

namespace ThatPiSample.Services
{
    public class BeaconService
    {
        private ConcurrentDictionary<string, BeaconSighting> _beacons;
        private List<Beacon> _beaconsToMonitor;

        private BluetoothLEAdvertisementWatcher _watcher;

        public BeaconService()
        {
            _beacons = new ConcurrentDictionary<string, BeaconSighting>();
        }

        public bool IsStarted { get; set; }

        public void Start(List<Beacon> beaconsToMonitor)
        {
            _beaconsToMonitor = beaconsToMonitor;

            if (_watcher != null)
            {
                _watcher.Received -= Receive_Watcher_Notification;
                _watcher.Stop();
                _watcher = null;
            }

            ClearBeacons();
            _watcher = new BluetoothLEAdvertisementWatcher()
            {
                ScanningMode = BluetoothLEScanningMode.Passive,
                // EXPLORE: Are there other useful properties here you could set
                //          to control how sensitive your device is?
            };
            _watcher.Received += Receive_Watcher_Notification;
            _watcher.Stopped += _watcher_Stopped;
            _watcher.Start();

            IsStarted = true;
        }

        private void _watcher_Stopped(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementWatcherStoppedEventArgs args)
        {
            System.Diagnostics.Debug.WriteLine("Stopped BLE Advertisement Watcher");
        }

        public void Stop()
        {
            if (_watcher != null)
            {
                _watcher.Received -= Receive_Watcher_Notification;
                _watcher.Stop();
                _watcher = null;
            }

            IsStarted = false;
        }

        private async void Receive_Watcher_Notification(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementReceivedEventArgs args)
        {
            await RegisterBeaconSighting(args.Advertisement, args.BluetoothAddress, args.Timestamp, args.RawSignalStrengthInDBm);
        }

        public async Task<bool> RegisterBeaconSighting(BluetoothLEAdvertisement advertisement, ulong address, DateTimeOffset timestamp, short signalStrength)
        {
            var beaconData = ExtractBeaconDataFromAdvertisement(advertisement);
            var sighting = ExtractSightingFromBeaconData(beaconData, address, timestamp, signalStrength);
            if (String.IsNullOrWhiteSpace(sighting.Namespace) || String.IsNullOrWhiteSpace(sighting.Instance))
            {
                return false;
            }

            var beaconId = $"{sighting.Namespace}-{sighting.Instance}";

            _beacons.AddOrUpdate(beaconId, sighting, (key, existing) =>
            {
                existing.SignalStrength = sighting.SignalStrength;
                existing.BaseTransmitPower = sighting.BaseTransmitPower ?? existing.BaseTransmitPower;
                existing.BatteryVoltage = sighting.BatteryVoltage ?? existing.BatteryVoltage;
                existing.LastSeen = sighting.LastSeen;
                existing.PublishedUrl = sighting.PublishedUrl ?? existing.PublishedUrl;
                existing.Temperature = sighting.Temperature ?? existing.Temperature;

                return existing;
            });

            return true;
        }

        public void ClearBeacons()
        {
            _beacons.Clear();
        }

        public IEnumerable<Beacon> GetVisibleBeacons(DateTimeOffset visibleOnOrAfter)
        {
            return _beacons.Values
                           .Where(v => v.LastSeen >= visibleOnOrAfter)
                           .Select(v =>
                           {
                               var beaconToMonitor = _beaconsToMonitor.FirstOrDefault(b => b.Identifier == $"{v.Namespace}-{v.Instance}");
                               if (beaconToMonitor != null)
                               {
                                   beaconToMonitor.SignalStrength = v.SignalStrength;
                               }

                               return beaconToMonitor;
                           })
                           .Where(b => b != null)
                           .ToList();
        }

        private BeaconSighting ExtractSightingFromBeaconData(BeaconData beaconData, ulong address, DateTimeOffset lastSeen, double distance)
        {
            var newSighting = new BeaconSighting
            {
                Address = address,
                Uuid = GetUuidByPrefix("20160809", beaconData),
                LastSeen = lastSeen,
                SignalStrength = distance
            };

            if (beaconData.DataSections != null && beaconData.DataSections.Count > 0)
            {
                foreach (var dataSection in beaconData.DataSections)
                {
                    if (dataSection.DataType == 0x16)
                    {
                        var data = dataSection.RawData;

                        // This appears to be how the Gimbal Beacons send us the URL
                        if (data.Length >= 3 && data[0] == 0xAA && data[1] == 0xFE)
                        {
                            if (data.Length > 4 && data[2] == 0x10 && data[4] <= 0x03)
                            {
                                var prefix = new string[]
                                {
                                "http://www.",
                                "https://www.",
                                "http://",
                                "https://"
                                };

                                newSighting.BaseTransmitPower = (sbyte)data[3];
                                newSighting.PublishedUrl = new Uri(prefix[data[4]] + (data.Length > 5 ? System.Text.Encoding.UTF8.GetString(data, 5, data.Length - 5) : String.Empty));
                            }
                            else if (data[2] == 0x20 && data.Length >= 16)
                            {
                                newSighting.BatteryVoltage = BitConverter.ToInt16(new[] { data[5], data[4] }, 0);
                                newSighting.Temperature = Convert.ToInt32(data[6] + ((float)data[7] / 256f));
                            }
                            else if (data[2] == 0x00 && data.Length >= 20)
                            {
                                // this is the namespace/instance broadcast
                                // This doesn't work right, so I'm just fixing it at -60 dBm (measured value on iPad)
                                //newSighting.BaseTransmitPower = (sbyte)data[3]; // (sbyte)((sbyte)data[3] + 41); // TX power at 0 meters
                                newSighting.BaseTransmitPower = -60;
                                newSighting.Namespace = BitConverter.ToString(data, 4, 10).Replace("-", "");
                                newSighting.Instance = BitConverter.ToString(data, 14, 6).Replace("-", "");
                            }
                        }
                    }
                }
            }

            if (String.IsNullOrWhiteSpace(newSighting.Namespace) && beaconData.ManufacturerData != null && beaconData.ManufacturerData.Count > 0)
            {
                foreach (var manufacturerData in beaconData.ManufacturerData)
                {
                    // This appears to be Kontakt
                    if (manufacturerData.CompanyId == 0x4C &&
                        manufacturerData.Data.Length >= 18 &&
                        manufacturerData.Data[0] == 0x02 &&
                        manufacturerData.Data[1] == 0x15)
                    {
                        newSighting.BaseTransmitPower = (sbyte)manufacturerData.Data[manufacturerData.Data.Length - 1];
                        var uuidString = BitConverter.ToString(manufacturerData.Data, 2, 16).Replace("-", "");
                        newSighting.Uuid = Guid.Parse(uuidString);
                        newSighting.Namespace = newSighting.Uuid.ToString("D").Substring(0, 23);
                        newSighting.Instance = newSighting.Uuid.ToString("D").Substring(24, 12);
                    }
                }
            }

            return newSighting;
        }

        private Guid GetUuidByPrefix(string prefix, BeaconData beaconData)
        {
            var candidateGuid = beaconData.ServiceUuids.Where(id => id.ToString().StartsWith(prefix, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
            if (candidateGuid != Guid.Empty)
            {
                return candidateGuid;
            }

            // Search through the beacon data to find the UUID
            var manuID = beaconData.ManufacturerData.Where(md => md.CompanyId == 0x4C).FirstOrDefault();
            if (manuID != null)
            {
                var guidStr = BitConverter.ToString(manuID.Data.Reverse().ToArray()).Replace("-", "");
                if (Guid.TryParse(guidStr, out candidateGuid) && guidStr.StartsWith(prefix))
                {
                    return candidateGuid;
                }
            }

            // Otherwise, hope it's in the data sections
            var dataSection = beaconData.DataSections.FirstOrDefault(ds => ds.DataType == 0x02 || ds.DataType == 0x03);
            if (dataSection != null)
            {
                var guidStr = BitConverter.ToString(dataSection.RawData.Reverse().ToArray()).Replace("-", "");
                if (Guid.TryParse(guidStr, out candidateGuid) && guidStr.StartsWith(prefix))
                {
                    return candidateGuid;
                }
            }

            // Don't know how to get the data...
            return Guid.Empty;
        }

        private BeaconData ExtractBeaconDataFromAdvertisement(BluetoothLEAdvertisement advertisement)
        {
            var retVal = new BeaconData()
            {
                ServiceUuids = (advertisement.ServiceUuids ?? new List<Guid>()).Select(u => u).ToList(),
                ManufacturerData = new List<ManufacturerData>((advertisement.ManufacturerData ?? new List<BluetoothLEManufacturerData>())
                .Select(md =>
                {
                    var data = new byte[md.Data.Length];
                    using (var reader = DataReader.FromBuffer(md.Data))
                    {
                        reader.ReadBytes(data);
                    }

                    return new ManufacturerData
                    {
                        CompanyId = md.CompanyId,
                        Data = data
                    };
                })),
                DataSections = new List<SectionData>((advertisement.DataSections ?? new List<BluetoothLEAdvertisementDataSection>())
               .Select(ds =>
               {
                   var data = new byte[ds.Data.Length];
                   using (var reader = DataReader.FromBuffer(ds.Data))
                   {
                       reader.ReadBytes(data);
                   }

                   return new SectionData
                   {
                       DataType = ds.DataType,
                       RawData = data
                   };
               }))
            };

            return retVal;
        }

        private class BeaconData
        {
            public List<Guid> ServiceUuids { get; set; }
            public List<ManufacturerData> ManufacturerData { get; set; }
            public List<SectionData> DataSections { get; set; }
        }

        private class ManufacturerData
        {
            public ushort CompanyId { get; set; }
            public byte[] Data { get; set; }
        }

        private class SectionData
        {
            public ushort DataType { get; set; }
            public byte[] RawData { get; set; }
        }

    }
}
