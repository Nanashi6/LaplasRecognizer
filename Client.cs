using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Windows;

namespace TCP_Client;

public class Client
{
    private Socket _client;
    public string Address
    {
        get => _client.LocalEndPoint.ToString(); 
    }

    public Client(IPEndPoint address)
    {
        _client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        _client.Connect(address);
        _client.ReceiveTimeout = 1000;
    }

    public async Task<DataForClient> SendImage(byte[] imageBytes, byte threading)
    {
        List<byte> message = new List<byte>();
        try
        {
            message.Add(threading);
            message.AddRange(BitConverter.GetBytes(imageBytes.Length));
            message.AddRange(imageBytes);
            await _client.SendAsync(message.ToArray());
            DataForClient result = await ReceiveImageWithTimeAsync();
            return result;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"{ex.ToString()}", "Ошибка");
        }

        return null!;
    }

    private async Task<DataForClient> ReceiveImageWithTimeAsync()
    {
        List<byte> data = new List<byte>();
        byte[] buffer = new byte[1024];
        int bytes = -1;
        int receivedBytes = 0;
        bool isFirstReceive = true;
        int messageLength = 0;
            
        do
        {
            try
            {
                bytes = await _client.ReceiveAsync(buffer, SocketFlags.None);
                // bytes = await _client.Receive(buffer, 0, buffer.Length, SocketFlags.None);
                receivedBytes += bytes;
            }
            catch (SocketException e)
            {
                MessageBox.Show($"{e.Message} пупу");
            }

            // Console.WriteLine(receivedBytes + " " + messageLength);
            
            if (isFirstReceive)
            {
                messageLength = BitConverter.ToInt32(buffer[0..(sizeof(Int32))]);
                data.AddRange(buffer[(sizeof(Int32))..buffer.Length]);
                isFirstReceive = false;
                receivedBytes -= 4;
            }
            else data.AddRange(buffer[0..bytes]);
        } while (receivedBytes < messageLength);
        
        DataForClient dataForClient = JsonSerializer.Deserialize<DataForClient>(new MemoryStream(data.ToArray()));
        return dataForClient;
    }

    public void Disconnect()
    {
        _client.Disconnect(false);
    }
}