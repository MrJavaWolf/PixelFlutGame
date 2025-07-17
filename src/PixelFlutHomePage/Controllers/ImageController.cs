using Microsoft.AspNetCore.Mvc;
using PixelFlut.Core;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace PixelFlutHomePage.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ImageController(
    PixelFlutServiceProvider pixelFlutServiceProvider) : ControllerBase
{
    [HttpGet]
    public IActionResult GetCurrentImage()
    {
        IPixelFlutScreenProtocol pixelFlutScreenProtocol = pixelFlutServiceProvider.ServiceProvider.GetRequiredService<IPixelFlutScreenProtocol>();
        PixelFlutScreen pixelFlutScreen = pixelFlutServiceProvider.ServiceProvider.GetRequiredService<PixelFlutScreen>();
        PixelFlutScreenConfiguration configuration = pixelFlutServiceProvider.ServiceProvider.GetRequiredService<PixelFlutScreenConfiguration>();
        Image<Rgba32> image = new Image<Rgba32>(configuration.ResolutionX, configuration.ResolutionY);
        var frame = pixelFlutScreen.CurrentFrame;
        foreach (PixelBuffer pixelBuffer in frame)
        {
            for (int i = 0; i < pixelBuffer.Buffers.Count; i++)
            {

                pixelFlutScreenProtocol.Draw(pixelBuffer.Buffers[i], image);
            }
        }

        MemoryStream memoryStream = new MemoryStream();
        image.SaveAsBmp(memoryStream);
        memoryStream.Position = 0;
        return File(memoryStream, "image/bmp");



    }

}



