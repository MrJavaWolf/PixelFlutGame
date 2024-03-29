# Pixelflut Games

Play [Pong](https://en.wikipedia.org/wiki/Pong) on a [Pixelflut](https://labitat.dk/wiki/Pixelflut) using a controller, works on both Linux and Windows.

Requirements:

- A Pixelflut server
- A Game Controller

## Get started

1. Install .NET 7
1. `cd src/PixelFlut`
1. Edit `src/PixelFlut/appsettings.json` to point to the Pixelflut server's IP address and port
1. `sudo dotnet run`
1. Make sure the Game Controller is connected
1. Press START on the Game Controller

You can now control the 2 players with the Game Controller(s).

## Controls

You can either use 1 or 2 controllers. If you use 

### 1 Controller

### Nitendo like game controller

| Input Id | Input  | Action |
| - | - | - |
| 65585 | Up | Moves left player up |
| 65585 | Down | Moves left player down |
| 589825 | X | Moves right player up |
| 589827 | B | Moves right player down |

### Playstation like game controller

| Input Id | Input  | Action |
| - | - | - |
| 65585 | Left stick | Moves left player up |
| 65585 | Left stick | Moves left player down |
| 589828 | Triangle | Moves right player up |
| 589826 | Cross | Moves right player down |

### 2 Controllers

### Nitendo like game controller

| Input Id | Input  | Action |
| - | - | - |
| 65585 | Up | Moves left player up |
| 65585 | Down | Moves left player down |

### Playstation like game controller

| Input Id | Input  | Action |
| - | - | - |
| 65585 | Left stick | Moves left player up |
| 65585 | Left stick | Moves left player down |

## Configuration

The configurations are stored in `src/PixelFlut/appsettings.json`.

You can change a range of different settings like:

- The size and offset of the play area
- The size of the player / ball
- The speed of the player / ball
- Number of threads that will be spamming the Pixelflut and other tweeking values

## Links
- Official wiki: https://labitat.dk/wiki/Pixelflut 
- DO NOT TRUST THE PROTOCOL DOCUMENTATION: https://github.com/JanKlopper/pixelvloed/blob/master/protocol.md
- Only trust the server code: https://github.com/JanKlopper/pixelvloed/blob/master/C/Server/main.c 
- The server: https://github.com/JanKlopper/pixelvloed
- A example client: https://github.com/Hafpaf/pixelVloedClient 


