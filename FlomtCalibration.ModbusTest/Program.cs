using FlomtCalibration.Modbus;
using System.IO.Ports;

// var modbusProtocol = new ModbusProtocolTcp("192.168.8.22", 5000);
var modbusProtocol = new ModbusProtocolTcp("185.147.58.54", 5000);
// var modbusProtocol = new ModbusProtocolSerial("COM3", 57600, Parity.None, 8, StopBits.One);

var bytes = await modbusProtocol.ReadRegistersBytesAsync(1, 32740, 10);

Console.WriteLine(string.Join(" ", bytes ?? Array.Empty<byte>()));