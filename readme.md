# Pixelflut Games

Play [Pong](https://en.wikipedia.org/wiki/Pong) on a [Pixelflut](https://labitat.dk/wiki/Pixelflut) using a controller, works on both Linux and Windows.

Requirements:

- A Pixelflut server
- A Game Controller

## Get started

1. [Install .NET 6](https://dotnet.microsoft.com/en-us/download/dotnet/6.0) ([or later](https://dotnet.microsoft.com/en-us/download/dotnet))
2. `cd src/PixelFlut`
3. Edit `appsettings.json` to point to the correct IP address and port
4. `dotnet run`

You can now control the 2 paddels with the Game Controller

## Configuration

The configurations are stored in `src/PixelFlut/appsettings.json`.

## Links
- Official wiki: https://labitat.dk/wiki/Pixelflut 
- DO NOT TRUST THE PROTOCOL DOCUMENTATION: https://github.com/JanKlopper/pixelvloed/blob/master/protocol.md
- Only trust the server code: https://github.com/JanKlopper/pixelvloed/blob/master/C/Server/main.c 
- The server: https://github.com/JanKlopper/pixelvloed
- A example client: https://github.com/Hafpaf/pixelVloedClient 


