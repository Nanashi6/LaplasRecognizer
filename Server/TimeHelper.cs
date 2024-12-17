using System.Diagnostics;

public class TimeHelper
{
	private Stopwatch _timer;

	public TimeHelper()
	{
		_timer = new Stopwatch();
	}

	public void Start() => _timer.Restart();

	public long Stop()
	{
		_timer.Stop();
		return _timer.ElapsedMilliseconds;
	}
}