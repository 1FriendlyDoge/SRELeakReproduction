using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Speech.AudioFormat;
using System.Speech.Recognition;

namespace SRELeakReproduction;

class Program
{
    public static async Task Main(string[] args)
    {
        if(!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Console.WriteLine("SocketWorker is only supported on Windows.");
            return;
        }
        
        using (Socket server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)) 
        {
            Console.WriteLine("Starting Socket Server...");
            
            server.Bind(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 6969));
            server.Listen(100);
            
            while (true)
            {
                Socket client = await server.AcceptAsync();

                #pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                Task.Run(() => HandleClient(client)).ConfigureAwait(false);
                #pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            }
        }
    }
    
    private static void HandleClient(Socket client)
    {
        try
        {
            Stream bufferedByteStream = new BufferedByteStream(48000);
            using (Stream originStream = new NetworkStream(client, true))
            {
                Console.WriteLine("Accepted Client");
                // Send PCM (user [8bytes]; guild [8bytes]; wave [?bytes])
                byte[] userIdBytes = new byte[8];
                byte[] guildIdBytes = new byte[8];
                Console.WriteLine("Buffer allocated");
                int user_bytes_read = originStream.Read(userIdBytes);
                int guild_bytes_read = originStream.Read(guildIdBytes);
                
                Console.WriteLine("Wrote to buffer");
                if (user_bytes_read != 8 || guild_bytes_read != 8)
                {
                    Console.WriteLine("Exit Condition: Bytes missing!");
                    return;
                }

                long userId = BitConverter.ToInt64(userIdBytes);
                long guildId = BitConverter.ToInt64(guildIdBytes);
                
                Console.WriteLine($"Connected: {userId} - {guildId}");
                
                byte[] buffer = new byte[48000];
                
                using (SpeechRecognitionEngine engine = new SpeechRecognitionEngine())
                {
                    engine.LoadGrammar(new Grammar(new GrammarBuilder("test")));
                    engine.SpeechRecognized += HandleSpeechRecognized;
                    engine.SetInputToAudioStream(bufferedByteStream, new SpeechAudioFormatInfo(EncodingFormat.Pcm, 48000, 16, 2, 192000, 4, null));
                    engine.RecognizeAsync(RecognizeMode.Multiple);
                    bool disposed = false;
                    
                    while (!disposed)
                    {
                        Thread.Sleep(100);
                        try
                        {
                            int read = originStream.Read(buffer, 0, 48000);
                            bufferedByteStream.Write(buffer, 0, read);
                        }
                        catch (IOException ex) when (ex.InnerException is SocketException { SocketErrorCode: SocketError.ConnectionReset })
                        {
                            Console.WriteLine("Connection was forcibly closed by the remote host.");
                            bufferedByteStream.Close();
                            disposed = true;
                        }
                    }
                }
                Console.WriteLine("Disposed Engine");
            }
            bufferedByteStream.Close();
            Console.WriteLine("Disposed Streams");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
        finally
        {
            Console.WriteLine("Done");
        }
    }
    
    private static void HandleSpeechRecognized(object? sender, SpeechRecognizedEventArgs e)
    {
        Console.WriteLine($"{e.Result.Grammar.Name} [{e.Result.Confidence}]: {e.Result.Text}");
    }
}