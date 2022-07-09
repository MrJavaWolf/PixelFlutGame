namespace PixelFlut.Core
{
    public class PixelBufferFactory
    {
        private readonly IPixelFlutScreenProtocol screenProtocol;
        public PixelFlutScreenConfiguration Screen { get; }

        public PixelBufferFactory(
            IPixelFlutScreenProtocol screenProtocol,
            PixelFlutScreenConfiguration screenConfiguration)
        {
            this.screenProtocol = screenProtocol;
            this.Screen = screenConfiguration;
        }

        public PixelBuffer Create(int numberOfPixels)
        {
            return new PixelBuffer(numberOfPixels, screenProtocol, Screen);
        }
    }
}
