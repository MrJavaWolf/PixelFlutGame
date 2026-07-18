using PixelFlut.Core;
using System.Text;

namespace pixelflut.TestShader;

internal class TestShaderGame : IGame
{
    public class Configuration
    {
        /// <summary>
        /// Whether the test image should be moving
        /// </summary>
        public bool Moving { get; set; }

        /// <summary>
        /// Change the still image's offset
        /// </summary>
        public double TestImageOffset { get; set; }

        /// <summary>
        /// Some math variable...
        /// </summary>
        public float Multiplier { get; set; }

        /// <summary>
        /// The path to the shader to use
        /// </summary>
        public required string Shader { get; set; }

        /// <summary>
        /// The kernel name (method) to call in the shader 
        /// </summary>
        public required string KernelName { get; set; } = "process_buffer";
    }

    private readonly Configuration config;
    private readonly ILogger<TestShaderGame> logger;
    private readonly PixelBufferFactory bufferFactory;
    private readonly PixelFlutScreenProtocol0_Shader screenProtocol;
    private List<PixelBuffer> frame;
    private readonly OpenClProgram openClProgram;
    private Memory<byte> fullBufferMemory;
    public TestShaderGame(
        Configuration config,
        ILogger<TestShaderGame> logger,
        PixelBufferFactory bufferFactory,
        IPixelFlutScreenProtocol screenProtocol)
    {
        this.config = config;
        this.logger = logger;
        this.bufferFactory = bufferFactory;
        this.screenProtocol = screenProtocol as PixelFlutScreenProtocol0_Shader ??
            throw new NotSupportedException($"Shader only supports the {nameof(PixelFlutScreenProtocol0_Shader)}");

        // Creates the pixel buffer
        this.logger.LogInformation($"Creates pixel buffer for the {this.GetType().Name}...");
        this.logger.LogInformation("Config: {@config}", config);

        List<Memory<byte>> memoryBuffers = this.screenProtocol.CreateBuffers();

        PixelBuffer buffer = new PixelBuffer(
            bufferFactory.Screen.ResolutionX * bufferFactory.Screen.ResolutionY,
            screenProtocol,
            memoryBuffers);
        fullBufferMemory = this.screenProtocol.FullBuffer.AsMemory();
        frame = [buffer];


        string fullShaderPath = Path.GetFullPath(config.Shader);
        logger.LogInformation($"Loads shader '{config.Shader}' from file: '{fullShaderPath}'");

        string kernelSource = File.ReadAllText(fullShaderPath, Encoding.UTF8);

        openClProgram = new(
            kernelSource: kernelSource,
            kernelName: config.KernelName,
            inputBufferSize: this.screenProtocol.FullBuffer.Length);

        // Initializes the test image
        DrawRainBowTestImage(new GameTime() { TotalTime = TimeSpan.FromSeconds(config.TestImageOffset) });
        this.logger.LogInformation($"Pixel buffer for the {this.GetType().Name} is ready");
    }

    public List<PixelBuffer> Loop(GameTime time, IReadOnlyList<IGamePadDevice> gamePads)
    {
        DrawRainBowTestImage(time);
        return frame;
    }

    public void DrawRainBowTestImage(GameTime time)
    {
        openClProgram.Run(
            fullBufferMemory.Span,
            width: bufferFactory.Screen.ResolutionX,
            height: bufferFactory.Screen.ResolutionY,
            multiplier: config.Multiplier,
            offset: (float)time.TotalTime.TotalSeconds);

    }
}
