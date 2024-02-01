using System.Net.Sockets;
using NAudio.Wave;

var waveFormat = new WaveFormat(48000, 16, 2);
Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
var waveInEvent = new WaveInEvent();

byte[] b = BitConverter.GetBytes(0L).Concat(BitConverter.GetBytes(0L)).ToArray();
client.Connect("localhost", 6969);
client.Send(b);
Console.WriteLine("Connected");
waveInEvent.WaveFormat = waveFormat;

waveInEvent.DataAvailable += (sender, e) => client.Send(e.Buffer);

waveInEvent.StartRecording();
Console.WriteLine("Press 'Enter' to stop recording and exit...");
Console.ReadLine();
            
waveInEvent.StopRecording();
waveInEvent.Dispose();
client.Close();