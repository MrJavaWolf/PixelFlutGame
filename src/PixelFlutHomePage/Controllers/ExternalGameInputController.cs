using Microsoft.AspNetCore.Mvc;
using PixelFlutHomePage.Services;

namespace PixelFlutHomePage.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ExternalGameInputController(ExternalGameInputService externalGameInputService) : ControllerBase
{
    public record GameControllerState(string name, ExternalGameControllerInput State);
    [HttpGet]
    public IEnumerable<GameControllerState> GetConnectedGameControllers()
    {
        return externalGameInputService.ConnectedGameControllers.Select(x => new GameControllerState(x.Item1, x.Item2));
    }

    [HttpPost]
    public void SendGameInput(
        [FromQuery] string controllerId,
        [FromBody] ExternalGameControllerInputDto controllerState)
    {
        externalGameInputService.UpdateGameInput(controllerId, controllerState);
    }
}



