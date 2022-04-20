// Official wiki: https://labitat.dk/wiki/Pixelflut 
// DO NOT TRUST THE PROTOCOL DOCUMENTATION: https://github.com/JanKlopper/pixelvloed/blob/master/protocol.md
// Only trust the server code: https://github.com/JanKlopper/pixelvloed/blob/master/C/Server/main.c 
// The server: https://github.com/JanKlopper/pixelvloed
// A example client: https://github.com/Hafpaf/pixelVloedClient 

using HidSharp;
using System.Net;
using System.Net.Sockets;


//TestProtocol_2_Bit_setup();
//return;
Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

IPAddress serverAddr = IPAddress.Parse("10.42.1.12");

IPEndPoint endPoint = new IPEndPoint(serverAddr, 5005);

short screenXStart = 600;
short screenXEnd = 850; // 1650;
//short screenXEnd = 512;
short screenYStart = 300;
short screenYEnd = 600; //1200;
//short screenYEnd = 512;


int myOffsetX = 0;
int myOffsetY = 0;
Console.WriteLine("Hello world of pixel flut");
DeviceList.Local.Changed += Local_Changed;
PrintDevices(); 



//while (true)
//{
//    GameLoopSample game = new GameLoopSample();
//    await game.ExecuteAsync();
//    if (Console.ReadLine() == "Y") break;
//}


//while(true) RunProtocol_1();
Console.ReadLine();
Console.WriteLine("Done");


void Local_Changed(object? sender, DeviceListChangedEventArgs e)
{
    Console.WriteLine("Connected devices changed");

    PrintDevices();
}


(byte r, byte g, byte b) GetColor(int x, int y)
{
    // R
    int leftOverRed = (y + myOffsetY) % 256;
    byte red = leftOverRed < 128 ?
        (byte)leftOverRed :
        (byte)(256 - leftOverRed);

    // G

    // B
    int leftOverBlue = (x + myOffsetX) % 256;
    byte blue = leftOverBlue < 128 ?
        (byte)leftOverBlue :
        (byte)(256 - leftOverBlue);
    return (red, 0, blue);
    //return (255, 255, 255);
}


void RunProtocol_1()
{
    myOffsetX -= 10;
    myOffsetY -= 10;
    int numberOfPixel = 140;
    int bytesPerPixel = 8;
    byte[] send_buffer = new byte[2 + numberOfPixel * bytesPerPixel];
    send_buffer[0] = 0x01; // Protocol 1
    send_buffer[1] = 0x00; // Not used
                           // Maximum number of pixel: 186
    Console.WriteLine("Protocol 1: Sending...");

    for (int y = screenYStart; y < screenYEnd; y++)
    {
        for (int xStart = screenXStart; xStart < screenXEnd; xStart += numberOfPixel)
        {
            int pixelNumber = 0;
            for (int x = xStart; x < xStart + numberOfPixel; x++)
            {
                UpdateProtocolBuffer_1(send_buffer, 2 + pixelNumber * bytesPerPixel, x, y);
                pixelNumber++;
            }
            sock.SendTo(send_buffer, endPoint);
        }
    }
}

void UpdateProtocolBuffer_1(byte[] send_buffer, int offset, int x, int y)
{
    byte[] xBytes = BitConverter.GetBytes(x);
    byte[] yBytes = BitConverter.GetBytes(y);
    byte firstBitsMask = 15;
    send_buffer[offset + 0] = xBytes[0];
    send_buffer[offset + 1] = xBytes[1];
    send_buffer[offset + 2] = yBytes[0];
    send_buffer[offset + 3] = yBytes[1];
    (byte r, byte g, byte b) = GetColor(x, y);
    send_buffer[offset + 4] = (byte)r;
    send_buffer[offset + 5] = (byte)g;
    send_buffer[offset + 6] = (byte)b;
    send_buffer[offset + 7] = (byte)255;
}


void RunProtocol_0()
{
    byte[] send_buffer = new byte[9];
    send_buffer[0] = 0x00; // Protocol 0
    send_buffer[1] = 0x00; // Not used

    Console.WriteLine("Protocol 0: Sending...");
    while (true)
    {
        for (short y = screenYStart; y < screenYEnd; y++)
        {
            for (short x = screenXStart; x < screenXEnd; x++)
            {
                byte[] xBytes = BitConverter.GetBytes(x);
                byte[] yBytes = BitConverter.GetBytes(y);
                //x
                send_buffer[2] = xBytes[0];
                send_buffer[3] = xBytes[1];

                //y
                send_buffer[4] = yBytes[0];
                send_buffer[5] = yBytes[1];

                // R
                int leftOverRed = y % 256;
                byte red = 0;
                if (leftOverRed < 128)
                {
                    red = (byte)leftOverRed;
                }
                else
                {
                    red = (byte)(256 - leftOverRed);
                }
                send_buffer[6] = red;

                // G

                // B
                int leftOverBlue = x % 256;
                byte blue = 0;
                if (leftOverBlue < 128)
                {
                    blue = (byte)leftOverBlue;
                }
                else
                {
                    blue = (byte)(256 - leftOverBlue);
                }
                send_buffer[8] = (byte)blue;

                send_buffer[6] = (byte)0;
                send_buffer[7] = (byte)0;
                send_buffer[8] = (byte)0;

                sock.SendTo(send_buffer, endPoint);
            }
        }
    }
}

// Protocol 2 does not work currently, not sure why...
void RunProtocol_2()
{
    int numberOfPixel = 140; // Maximum number of pixel: 140
    int bytesPerPixel = 6;
    byte[] send_buffer = new byte[2 + numberOfPixel * bytesPerPixel];
    send_buffer[0] = 0x01; // Protocol 1
    send_buffer[1] = 0x00; // Not used

    Console.WriteLine("Protocol 2: Sending...");
    for (int y = 0; y < screenYEnd; y++)
    {
        for (int x = 0; x < numberOfPixel; x++)
        {
            UpdateProtocolBuffer_2(send_buffer, 2 + x * bytesPerPixel, x, y);
        }
        sock.SendTo(send_buffer, endPoint);
    }
}

void UpdateProtocolBuffer_2(byte[] send_buffer, int offset, int x, int y)
{
    byte[] xBytes = BitConverter.GetBytes(x);
    byte[] yBytes = BitConverter.GetBytes(y);
    byte firstBitsMask = 0x0F;
    byte lastBitsMask = 0xF0;
    send_buffer[offset + 0] = xBytes[0];
    send_buffer[offset + 1] = (byte)(((xBytes[1] & firstBitsMask) << 4) | (yBytes[0] & firstBitsMask));
    send_buffer[offset + 2] = (byte)((yBytes[0] >> 4) | (yBytes[1] << 4));
    send_buffer[offset + 3] = (byte)0;
    send_buffer[offset + 4] = (byte)255;
    send_buffer[offset + 5] = (byte)0;
}


void TestProtocol_2_Bit_setup()
{
    // Test code to test the bits are setup correctly for protocol 1
    uint v1 = 1234;
    uint v2 = 2245;
    byte[] xBytes = BitConverter.GetBytes(v1);
    byte[] yBytes = BitConverter.GetBytes(v2);
    byte firstBitsMask = 0x0F;
    byte lastBitsMask = 0xF0;

    Console.WriteLine($"xBytes[0]: {Convert.ToString(xBytes[0], toBase: 2)}, >> {Convert.ToString((byte)(xBytes[0] >> 4), toBase: 2)}, << {Convert.ToString((byte)(xBytes[0] << 4), toBase: 2)}");
    Console.WriteLine($"xBytes[1]: {Convert.ToString(xBytes[1], toBase: 2)}, >> {Convert.ToString((byte)(xBytes[1] >> 4), toBase: 2)}, << {Convert.ToString((byte)(xBytes[1] << 4), toBase: 2)}");
    Console.WriteLine($"YBytes[0]: {Convert.ToString(yBytes[0], toBase: 2)}, >> {Convert.ToString((byte)(yBytes[0] >> 4), toBase: 2)}, << {Convert.ToString((byte)(yBytes[0] << 4), toBase: 2)}");
    Console.WriteLine($"YBytes[1]: {Convert.ToString(yBytes[1], toBase: 2)}, >> {Convert.ToString((byte)(yBytes[1] >> 4), toBase: 2)}, << {Convert.ToString((byte)(yBytes[1] << 4), toBase: 2)}");

    Console.WriteLine("Combined 1: " + Convert.ToString((byte)(((xBytes[1] & firstBitsMask) << 4) | (yBytes[0] & firstBitsMask)), toBase: 2));
    Console.WriteLine("Combined 2: " + Convert.ToString((byte)((yBytes[0] >> 4) | (yBytes[1] << 4)), toBase: 2));
    Console.WriteLine("Combined 3: " + Convert.ToString((byte)((yBytes[0] & lastBitsMask << 4) | (yBytes[1] & firstBitsMask >> 4)), toBase: 2));


    //send_buffer[offset + 1] = ;
    //send_buffer[offset + 2] = (byte)((yBytes[0] & lastBitsMask << 4) | (yBytes[1] & firstBitsMask >> 4));
    byte[] valuesToSend = new byte[3];
    valuesToSend[0] = xBytes[0];
    valuesToSend[1] = (byte)((xBytes[1] & firstBitsMask) | (yBytes[0] << 4));
    valuesToSend[2] = (byte)((yBytes[0] >> 4) | (yBytes[1] << 4));
}

void PrintDevices()
{
    int i = 0;
    foreach (var hidDevice in DeviceList.Local.GetHidDevices())
    {
        string friendlyName = "<Unknown name>";
        try
        {
            friendlyName = hidDevice.GetFriendlyName();

        }
        catch { }
        Console.WriteLine($"{i}: " +
            $"{friendlyName}, " +
            $"{hidDevice.DevicePath}, " +
            $"{hidDevice.VendorID}, " +
            $"{hidDevice.ProductID}" +
            $"{hidDevice.ReleaseNumber}" +
            $"{hidDevice.ReleaseNumberBcd}" +
            $"");

        i++;
    }
}