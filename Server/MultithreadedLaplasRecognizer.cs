using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

public class MultithreadedLaplasRecognizer : IImageProcessor
{
	private readonly int _numberOfThreads;
	public MultithreadedLaplasRecognizer(int numberOfThreads)
	{
		_numberOfThreads = numberOfThreads;
	}
	
	public Bitmap ProcessImage(Bitmap originalImage)
	{
		return Recognize(originalImage, _numberOfThreads);
	}

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Проверка совместимости платформы", Justification = "<Ожидание>")]
    private Bitmap Recognize(Bitmap originalImage, int numberOfThreads)
	{
		Bitmap processedImage = new Bitmap(originalImage.Width, originalImage.Height, PixelFormat.Format8bppIndexed);
    
	    ColorPalette palette = processedImage.Palette;
	    for (int i = 0; i < 256; i++)
	    {
	        palette.Entries[i] = Color.FromArgb(i, i, i);
	    }
	    processedImage.Palette = palette;

	    int imageHeight = originalImage.Height;
	    int imageWidth = originalImage.Width;
	    int rowsPerThread = imageHeight / numberOfThreads;
	    int extraRows = imageHeight % numberOfThreads;
	    
	    int[,] filterMatrix = {
	        { 1, 1, 1 },
	        { 1, -8, 1 },
	        { 1, 1, 1 }
	    };

        BitmapData srcData = originalImage.LockBits(
	        new Rectangle(0, 0, imageWidth, imageHeight),
	        ImageLockMode.ReadOnly,
	        PixelFormat.Format24bppRgb);
        BitmapData destData = processedImage.LockBits(
	        new Rectangle(0, 0, imageWidth, imageHeight),
	        ImageLockMode.WriteOnly,
	        PixelFormat.Format8bppIndexed);

        int srcBytesPerPixel = 3; // для 24bppRgb
	    int destBytesPerPixel = 1; // для 8bppIndexed
	    int srcStride = srcData.Stride;
	    int destStride = destData.Stride;

	    byte[] srcBuffer = new byte[srcStride * imageHeight];
	    byte[] destBuffer = new byte[destStride * imageHeight];

	    Marshal.Copy(srcData.Scan0, srcBuffer, 0, srcBuffer.Length);

	    Parallel.For(0, numberOfThreads, i =>
	    {
	        int startY = i * rowsPerThread;
	        int endY = (i == numberOfThreads - 1)
	            ? startY + rowsPerThread + extraRows
	            : startY + rowsPerThread;

	        int filterOffset = 1;

	        for (int y = Math.Max(startY, filterOffset); y < Math.Min(endY, imageHeight - filterOffset); y++)
	        {
	            for (int x = filterOffset; x < imageWidth - filterOffset; x++)
	            {
	                int sum = 0;
	                int srcOffset = y * srcStride + x * srcBytesPerPixel;
	                
	                for (int filterY = -filterOffset; filterY <= filterOffset; filterY++)
	                {
	                    for (int filterX = -filterOffset; filterX <= filterOffset; filterX++)
	                    {
	                        int calcOffset = srcOffset + 
	                            (filterX * srcBytesPerPixel) + 
	                            (filterY * srcStride);

	                        if (calcOffset >= 0 && calcOffset < srcBuffer.Length - 2)
	                        {
	                            int gray = (int)(
	                                srcBuffer[calcOffset] * 0.299 + 
	                                srcBuffer[calcOffset + 1] * 0.587 + 
	                                srcBuffer[calcOffset + 2] * 0.114);

	                            sum += gray * filterMatrix[filterY + filterOffset, filterX + filterOffset];
	                        }
	                    }
	                }

	                destBuffer[y * destStride + x] = ClampToByte(sum);
	            }
	        }
	    });

	    Marshal.Copy(destBuffer, 0, destData.Scan0, destBuffer.Length);
	    originalImage.UnlockBits(srcData);
	    processedImage.UnlockBits(destData);

	    return processedImage;
	}
	
	public byte ClampToByte(int value)
	{
		return (byte)(value < 0 ? 0 : (value > 255 ? 255 : value));
	}
}