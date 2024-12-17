namespace Server;

public class DataForClient
{
	public long Time { get; set; }
	public byte[] ImageData { get; set; } = Array.Empty<byte>();
}