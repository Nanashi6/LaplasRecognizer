using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

public class LinearLaplasRecognizer : IImageProcessor
{
	public Bitmap ProcessImage(Bitmap image)
	{
		return Recognize(image);
	}
	private Bitmap Recognize(Bitmap image)
	{
		int[,] filterMatrix = {
						{ 1, 1, 1 },
						{ 1, -8, 1 },
						{ 1, 1, 1 }
		};
		
		Bitmap filteredImage = new Bitmap(image.Width, image.Height, PixelFormat.Format8bppIndexed);
	    
	    ColorPalette palette = filteredImage.Palette;
	    for (int i = 0; i < 256; i++)
	    {
	        palette.Entries[i] = Color.FromArgb(i, i, i);
	    }
	    filteredImage.Palette = palette;

	    BitmapData srcData = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), 
	        ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
	    BitmapData destData = filteredImage.LockBits(new Rectangle(0, 0, filteredImage.Width, filteredImage.Height), 
	        ImageLockMode.WriteOnly, PixelFormat.Format8bppIndexed);

	    int srcBytesPerPixel = 3; // для 24bppRgb
	    int destBytesPerPixel = 1; // для 8bppIndexed
	    int srcStride = srcData.Stride;
	    int destStride = destData.Stride;
	    
	    IntPtr srcScan0 = srcData.Scan0;
	    IntPtr destScan0 = destData.Scan0;
	    
	    byte[] srcBuffer = new byte[Math.Abs(srcStride) * image.Height];
	    byte[] destBuffer = new byte[Math.Abs(destStride) * filteredImage.Height];
	    
	    Marshal.Copy(srcScan0, srcBuffer, 0, srcBuffer.Length);
	    
	    int filterOffset = 1;

	    for (int y = filterOffset; y < image.Height - filterOffset; y++)
	    {
	        for (int x = filterOffset; x < image.Width - filterOffset; x++)
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
	                    
	                    int gray = (int)(srcBuffer[calcOffset] * 0.299 + 
	                                     srcBuffer[calcOffset + 1] * 0.587 + 
	                                     srcBuffer[calcOffset + 2] * 0.114);
	                    
	                    sum += gray * filterMatrix[filterY + filterOffset, filterX + filterOffset];
	                }
	            }

	            destBuffer[y * destStride + x] = ClampToByte(sum);
	        }
	    }

	    Marshal.Copy(destBuffer, 0, destScan0, destBuffer.Length);
	    image.UnlockBits(srcData);
	    filteredImage.UnlockBits(destData);

	    return filteredImage;
	}
	
	public byte ClampToByte(int value)
	{
		return (byte)(value < 0 ? 0 : (value > 255 ? 255 : value));
	}
}