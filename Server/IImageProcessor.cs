using System.Drawing;

public interface IImageProcessor
{
	Bitmap ProcessImage(Bitmap image);
	byte ClampToByte(int value);
}