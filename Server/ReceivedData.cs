namespace Server;

public class ReceivedData
{
	public bool IsMultithreading { get; set; } = false;
	public List<byte> Data { get; set; } = new List<byte>();
	public bool Input { get; set; } = false;
	public bool SocketIsClosed { get; set; } =  false;
}