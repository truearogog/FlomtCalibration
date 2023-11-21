using Avalonia.Platform.Storage;
using FlomtCalibration.App.Extensions;
using FlomtCalibration.App.Models;
using FlomtCalibration.Modbus;
using Newtonsoft.Json;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace FlomtCalibration.App.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        #region Serial connection parameters

        public byte SlaveId
        {
            get => _slaveId;
            set => this.RaiseAndSetIfChanged(ref _slaveId, value);
        }
        private byte _slaveId;

        public string SerialPort
        {
            get => _serialPort;
            set => this.RaiseAndSetIfChanged(ref _serialPort, value);
        }
        private string _serialPort = string.Empty;
        [JsonIgnore]
        public ObservableCollection<string> SerialPortCollection { get; set; } = new();

        public int BaudRate
        {
            get => _baudRate;
            set => this.RaiseAndSetIfChanged(ref _baudRate, value);
        }
        private int _baudRate;
        [JsonIgnore]
        public ObservableCollection<int> BaudRateCollection { get; set; } = new();

        public Parity Parity
        {
            get => _parity;
            set => this.RaiseAndSetIfChanged(ref _parity, value);
        }
        private Parity _parity;
        [JsonIgnore]
        public ObservableCollection<Parity> ParityCollection { get; set; } = new();

        public int DataBits
        {
            get => _dataBits;
            set => this.RaiseAndSetIfChanged(ref _dataBits, value);
        }
        private int _dataBits;

        public StopBits StopBits
        {
            get => _stopBits;
            set => this.RaiseAndSetIfChanged(ref _stopBits, value);
        }
        private StopBits _stopBits;
        [JsonIgnore]
        public ObservableCollection<StopBits> StopBitsCollection { get; set; } = new();
        #endregion

        #region Calculation parameters
        public double Diameter
        {
            get => _diameter;
            set => this.RaiseAndSetIfChanged(ref _diameter, value);
        }
        private double _diameter;

        public double Pressure
        {
            get => _pressure;
            set => this.RaiseAndSetIfChanged(ref _pressure, value);
        }
        private double _pressure;
        #endregion

        // <temperature, (pressure, volume)>
        public Dictionary<double, Dictionary<double, double>> Tables = new();

        public ObservableCollection<CalculationResult> CalculationResults { get; set; } = new();

        public MainWindowViewModel()
        {
            CalculationResults = new(Enumerable.Range(0, 10).Select(x =>
                new CalculationResult
                {
                    DateTime = DateTime.Now,
                    Tc = x * 1000 * Random.Shared.NextDouble(),
                    fc = x * 1000 * Random.Shared.NextDouble(),
                    fmin = x * 1000 * Random.Shared.NextDouble(),
                    fmax = x * 1000 * Random.Shared.NextDouble(),
                    pc = x * 1000 * Random.Shared.NextDouble(),
                    tc = x * 1000 * Random.Shared.NextDouble(),
                    p2 = x * 1000 * Random.Shared.NextDouble(),
                    t2 = x * 1000 * Random.Shared.NextDouble(),
                    Vc = x * 1000 * Random.Shared.NextDouble(),
                    Qc = x * 1000 * Random.Shared.NextDouble(),
                    Uc = x * 1000 * Random.Shared.NextDouble(),
                }
            ));

            UpdateSerialPorts();
            ParityCollection = new(Enum.GetValues<Parity>());
            StopBitsCollection = new(Enum.GetValues<StopBits>());
            BaudRateCollection = new(new[] { 110, 300, 600, 1200, 2400, 4800, 9600, 14400, 19200, 38400, 57600, 115200, 128000, 256000 });
        }

        public async void Calculate()
        {
            try
            {
                var modbusProtocol = new ModbusProtocolSerial(SerialPort, BaudRate, Parity, DataBits, StopBits);
                var bytes = await modbusProtocol.ReadRegistersBytesAsync(SlaveId, 32740, 10);

                /*
                var bytes = new byte[]
                {
                    0x46, 0x11,
                    0x00, 0x00,
                    0xB6, 0x0B,
                    0xB6, 0x0B,
                    0xB6, 0x0B,
                    0x43, 0x00,
                    0x88, 0x07,
                    0x01, 0x00,
                    0x16, 0x00,
                    0xF2, 0x08,
                };
                */

                if (bytes != null)
                {
                    var Tc = BitConverter.ToUInt32(bytes.AsSpan()[0..4]) * 0.01d;
                    var com_fcPow = -BitConverter.ToUInt16(bytes.AsSpan()[14..16]);
                    var com_fc = Math.Pow(10, com_fcPow);
                    var fc = BitConverter.ToInt16(bytes.AsSpan()[4..6]) * com_fc;
                    var fmin = BitConverter.ToInt16(bytes.AsSpan()[6..8]) * com_fc;
                    var fmax = BitConverter.ToInt16(bytes.AsSpan()[8..10]) * com_fc;
                    var pc = BitConverter.ToInt16(bytes.AsSpan()[10..12]) * 0.001d;
                    var tc = BitConverter.ToInt16(bytes.AsSpan()[12..14]) * 0.01d;
                    var p2 = BitConverter.ToInt16(bytes.AsSpan()[16..18]) * 0.001d;
                    var t2 = BitConverter.ToInt16(bytes.AsSpan()[18..20]) * 0.01d;

                    void GetResult(double _Ve, double _Vc, double _Qc, double _Uc) => CalculationResults.Add(new()
                    {
                        DateTime = DateTime.Now,
                        Tc = Tc,
                        fc = fc,
                        fmin = fmin,
                        fmax = fmax,
                        pc = pc,
                        tc = tc,
                        p2 = p2,
                        t2 = t2,
                        Ve = _Ve,
                        Vc = _Vc,
                        Qc = _Qc,
                        Uc = _Uc,
                    });

                    void GetInvalidResult() => GetResult(double.NaN, double.NaN, double.NaN, double.NaN);

                    // get Ve using linear regression
                    var (tLess, tMore) = Tables.Keys.Between(t2);

                    if (!Tables.TryGetValue(tLess, out _) || !Tables.TryGetValue(tMore, out _))
                    {
                        GetInvalidResult();
                        return;
                    }

                    // calculate pressure for smaller temperature
                    var (pLess, pMore) = Tables[tLess].Keys.Between(p2);
                    if (!Tables[tLess].TryGetValue(pLess, out _) || !Tables[tLess].TryGetValue(pMore, out _))
                    {
                        GetInvalidResult();
                        return;
                    }
                    var veLess = Tables[tLess][pLess] + (Tables[tLess][pMore] - Tables[tLess][pLess]) * (p2 - pLess) / (pMore - pLess);

                    // calculate pressure for bigger temperature
                    (pLess, pMore) = Tables[tMore].Keys.Between(p2);
                    if (!Tables[tMore].TryGetValue(pLess, out _) || !Tables[tMore].TryGetValue(pMore, out _))
                    {
                        GetInvalidResult();
                        return;
                    }
                    var veMore = Tables[tMore][pLess] + (Tables[tMore][pMore] - Tables[tMore][pLess]) * (p2 - pLess) / (pMore - pLess);

                    // calculate resulting standart volume from volumes of 2 temperatures
                    var Ve = veLess + (veMore - veLess) * (t2 - tLess) / (tMore - tLess);

                    // normalize pressure and temperature
                    var ps = (pc + Pressure) / Pressure;
                    var pe = (p2 + Pressure) / Pressure;
                    var ts = tc + 273.15d;
                    var te = t2 + 273.15d;

                    // calucate resulting volume
                    var Vc = (Ve * ts * pe) / (te * ps);

                    var s = Math.PI * Diameter * Diameter * 0.25d;
                    var Uc = (Vc * 1_000_000) / (tc * s);
                    var Qc = Vc * 3600 / tc;

                    GetResult(Ve, Vc, Qc, Uc);
                }
            }
            catch (Exception)
            {

            }
        }

        public void ClearResults()
        {
            CalculationResults.Clear();
        }

        public async Task UpdateTablesFromFile(IStorageFile file)
        {
            Tables.Clear();
            CalculationResults.Clear();
            await using var stream = await file.OpenReadAsync();
            using var streamReader = new StreamReader(stream);
            // skip first line
            var line = await streamReader.ReadLineAsync();
            while ((line = await streamReader.ReadLineAsync()) != null)
            {
                var values = line.Split(',');
                for (var i = 0; i < values.Length / 3; i++)
                {
                    var p = double.Parse(values[i * 3], NumberStyles.Any, CultureInfo.InvariantCulture);
                    var t = double.Parse(values[i * 3 + 1], NumberStyles.Any, CultureInfo.InvariantCulture);
                    var v = double.Parse(values[i * 3 + 2], NumberStyles.Any, CultureInfo.InvariantCulture);
                    if (Tables.TryGetValue(t, out var table))
                    {
                        table[p] = v;
                    }
                    else
                    {
                        var _table = new Dictionary<double, double> { { p, v } };
                        Tables.Add(t, _table);
                    }
                }
            }
        }

        public async void SaveResultsToFile(IStorageFile file)
        {
            try
            {
                // Open writing stream from the file.
                await using var stream = await file.OpenWriteAsync();
                using var streamWriter = new StreamWriter(stream);

                var propertyInfos = typeof(CalculationResult).GetProperties();
                var titles = propertyInfos.Select(x =>
                {
                    var unit = x.GetCustomAttribute<UnitAttribute>()?.Unit;
                    return string.IsNullOrEmpty(unit) ? x.Name : x.Name + $"({unit})";
                });
                await streamWriter.WriteLineAsync(string.Join(',', titles));

                foreach (var calculationResult in CalculationResults)
                {
                    var values = propertyInfos.Select(x =>
                    {
                        var value = x.GetValue(calculationResult);
                        return value switch
                        {
                            double d => d.ToString("0.000"),
                            _ => value?.ToString()
                        };
                    });
                    await streamWriter.WriteLineAsync(string.Join(',', values));
                }
            }
            catch (Exception)
            {
            }
        }

        public void UpdateSerialPorts()
        {
            SerialPortCollection = new ObservableCollection<string>(System.IO.Ports.SerialPort.GetPortNames());
        }
    }
}