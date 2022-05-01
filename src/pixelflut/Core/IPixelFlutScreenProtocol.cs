﻿namespace PixelFlut.Core
{
    public interface IPixelFlutScreenProtocol
    {
        public int PixelPerBuffer { get; }
        public byte[] CreateBuffer();
        public void WriteToBuffer(byte[] send_buffer, int pixelNumber, int x, int y, byte r, byte g, byte b, byte a);
    }
}