using System.Drawing;
using System.Drawing.Imaging;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using Server;

public class TCPServer
{
	private readonly Socket _listener;
	private const int Port = 8081;
	private static int clientCounter = 0;
	private SemaphoreSlim _semaphore;

	public TCPServer()
	{
        //_timeHelper = new();
        _semaphore = new SemaphoreSlim(6);
        _listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
		_listener.ReceiveTimeout = 200;
	}
	
	public void Start()
	{
		_listener.Bind(new IPEndPoint(IPAddress.Any, Port));
		_listener.Listen();
		
		string RemoteIp = GetLocalIPAddress();
		
		Console.ForegroundColor = ConsoleColor.Blue;
		Console.WriteLine($"Listening for connections on: {RemoteIp}:{Port}");

		while (true)
		{
			Socket client = _listener.Accept();
			int clientId = ++clientCounter;
			Console.ForegroundColor = ConsoleColor.Cyan;
			Console.WriteLine("===============================================");
			Console.WriteLine($"Клиент #{clientId} подключен.");
			Console.WriteLine("===============================================");
			Console.ResetColor();
			Task.Run(() => HandleClient(client, clientId));
		}
	}
	
	private void HandleClient(Socket client, int clientId)
	{
		TimeHelper _timeHelper = new();
		try
		{
			while (client.Connected)
			{
				ReceivedData data = ReceiveMessage(client);

				if (!data.SocketIsClosed)
				{
					IImageProcessor imageProcessor = CreateImageProcessor(data.IsMultithreading);

					Bitmap receivedImage = new Bitmap(Image.FromStream(new MemoryStream(data.Data.ToArray())));
					Console.WriteLine($"Изображение получено от клиента #{clientId}.");

					_semaphore.Wait();
					_timeHelper.Start();
					Bitmap processedImage = imageProcessor.ProcessImage(receivedImage);
					long time = _timeHelper.Stop();
					_semaphore.Release();

					lock (this)
					{
						DisplayTimeInfo(time, clientId);
					}

					if (processedImage == null)
					{
						Console.WriteLine($"Ошибка: обработанное изображение пусто для клиента #{clientId}");
						break;
					}

					try
					{
						SendImage(client, processedImage, time);
					}
					catch (Exception ex)
					{
						Console.WriteLine($"Ошибка при отправке изображения клиенту #{clientId}: {ex.Message}");
						break;
					}

					//Console.WriteLine($"Обработанное изображение отправлено клиенту #{clientId}.");
				}
				else break;
			}
		}
		catch (SocketException ex)
		{
			Console.WriteLine($"Клиент {clientId} отключился");
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Ошибка у клиента #{clientId}: {ex.Message}");
		}
		finally
		{
			client.Close();
		}
	}
	
	private void DisplayTimeInfo(long elapsedMilliseconds, int clientId)
	{
		Console.ForegroundColor = ConsoleColor.Green;
		if (elapsedMilliseconds > 1000)
		{
			Console.WriteLine($"Время обработки для клиента #{clientId}: {elapsedMilliseconds / 1000.0:F2} секунд");
		}
		else
		{
			Console.WriteLine($"Время обработки для клиента #{clientId}: {elapsedMilliseconds} мс");
		}
		Console.ResetColor();
	}
	
	private IImageProcessor CreateImageProcessor(bool isMultithreading)
	{
		if (isMultithreading)
		{
			int numberOfThreads = 10;
			Console.WriteLine($"Выбран режим обработки: Многопоточный");
			return new MultithreadedLaplasRecognizer(numberOfThreads);
		}
		else
		{
			Console.WriteLine("Выбран режим обработки: Линейный");
			return new LinearLaplasRecognizer();
		}
	}
	
	private ReceivedData ReceiveMessage(Socket client)
	{
		bool isFirstReceive = true;
		int receivedBytes = 0;
		int bytes = -1;
		byte[] buffer = new byte[1024];
		int messageLength = 0;
		ReceivedData receivedData = new ReceivedData();
            
		do {
			try
			{
				bytes = client.Receive(buffer, 0, buffer.Length, SocketFlags.None);
				receivedBytes += bytes;
			}
			catch (SocketException e)
			{
				if (e.SocketErrorCode == SocketError.TimedOut)
				{
					if (receivedData.Data.Count > 0)
					{
						receivedData.Input = true;
						break;
					}
					receivedData.Input = false;
					continue;
				}
			}

			if (bytes == 0)
			{
				Console.WriteLine("Client is lost...");
				receivedData.SocketIsClosed = true;
				break;
			}

			if (bytes > 0)
			{
				if (isFirstReceive)
				{
					receivedData.IsMultithreading = BitConverter.ToBoolean(buffer[0..sizeof(byte)]);
					messageLength = BitConverter.ToInt32(buffer[sizeof(byte)..(sizeof(Int32) + sizeof(byte))]);
					receivedData.Data.AddRange(buffer[(sizeof(byte) + sizeof(Int32))..buffer.Length]);
					isFirstReceive = false;
					receivedBytes -= sizeof(byte) + sizeof(Int32);
				}
				else receivedData.Data.AddRange(buffer[0..bytes]);	
			}

		} while (!receivedData.Input || receivedBytes < messageLength);

		return receivedData;
	}
	
	private void SendImage(Socket client, Bitmap image, long time)
	{
		byte[] result;
		using (var stream = new MemoryStream())
		{
			image.Save(stream, ImageFormat.Jpeg);
			result = stream.ToArray();
		}
		
		DataForClient clientData = new DataForClient() { ImageData = result, Time = time };
		byte[] mes = JsonSerializer.SerializeToUtf8Bytes(clientData);
                    
		List<byte> message = new List<byte>();
		message.AddRange(BitConverter.GetBytes(mes.Length));
		message.AddRange(mes);

		client.Send(message.ToArray(), message.Count, SocketFlags.None);
	}
	
	private string GetLocalIPAddress()
	{
		string localIP = "Не удалось определить IP-адрес";
		try
		{
			var host = Dns.GetHostEntry(Dns.GetHostName());
			foreach (var ip in host.AddressList)
			{
				if (ip.AddressFamily == AddressFamily.InterNetwork)
				{
					localIP = ip.ToString();
					break;
				}
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Ошибка при получении локального IP: {ex.Message}");
		}
		return localIP;
	}
}