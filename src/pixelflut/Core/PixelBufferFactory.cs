namespace PixelFlut.Core
{
    public class PixelBufferFactory
    {
        private readonly IPixelFlutScreenProtocol screenProtocol;
        private readonly PixelFlutScreenConfiguration screenConfiguration;

        public PixelBufferFactory(
            IPixelFlutScreenProtocol screenProtocol,
            PixelFlutScreenConfiguration screenConfiguration)
        {
            this.screenProtocol = screenProtocol;
            this.screenConfiguration = screenConfiguration;
        }

        public PixelBuffer Create(int numberOfPixels)
        {
            return new PixelBuffer(numberOfPixels, screenProtocol, screenConfiguration);
        }
    }
}
