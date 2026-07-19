using pixelflut.Core;
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
        /// How fast the rainbows are moving
        /// </summary>
        public float Speed { get; set; }

        /// <summary>
        /// How big each rainbow is
        /// </summary>
        public float RainbowScale { get; set; }

        /// <summary>
        /// How much it should zoom
        /// </summary>
        public float ZoomMultiplier { get; set; }

        /// <summary>
        /// The path to the shader to use
        /// </summary>
        public required string Shader { get; set; }

        /// <summary>
        /// The kernel name (method) to call in the shader 
        /// </summary>
        public required string KernelName { get; set; } = "process_buffer";
    }

    private int[] Remapper;

    private readonly Configuration config;
    private readonly ILogger<TestShaderGame> logger;
    private readonly PixelBufferFactory bufferFactory;
    private readonly PixelFlutScreenProtocol0_Shader screenProtocol;
    private List<PixelBuffer> frame;
    private readonly OpenClProgram openClProgram;
    private Memory<byte> fullBufferMemory;

    private int currentCoordinateSet = 0;
    private List<(float, float)> interesstingCoordinates = [
        (-0.743643887037151f,  0.131825904205330f), // Seahorse Valley
        (-1.250660000000000f,  0.020120000000000f), // Mini Mandelbrot
        (-1.768778833000000f,  0.001738996000000f), // Large Mini Mandelbrot
        (-1.749900000000000f,  0.000000000000000f), // Needle
        (-0.745300000000000f,  0.112700000000000f), // Lightning
        (-0.761574000000000f, -0.084759600000000f), // Spiral Galaxy
        (-0.101100000000000f,  0.956300000000000f), // Double Spiral
        ( 0.001643721971154f, -0.822467633298876f), // Dendrite Forest
        (-1.941569000000000f,  0.000000000000000f), // Valley of Needles
        (-0.162000000000000f,  1.040000000000000f), // Snowflake
        (-0.123750000000000f,  0.565000000000000f), // Rabbit Valley
        (-0.775683770000000f,  0.136467370000000f), // Feather Valley
        (-0.745428000000000f,  0.113009000000000f), // Seahorse Filaments
        (-0.748000000000000f,  0.100500000000000f), // Spiral Ridges
        (-0.160701350000000f,  1.037566500000000f), // Snowflake Detail
        (-1.999250000000000f,  0.000000000000000f), // Left Antenna
        ( 0.274000000000000f,  0.482000000000000f), // Satellite Bulbs
        ];



    public TestShaderGame(
        Configuration config,
        ILogger<TestShaderGame> logger,
        PixelBufferFactory bufferFactory,
        FileLoader fileLoader,
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


        logger.LogInformation($"Loads shader '{config.Shader}'");
        byte[] fileContent = fileLoader.Load(config.Shader);
        string kernelSource = Encoding.UTF8.GetString(fileContent);

        Remapper = new int[bufferFactory.Screen.ResolutionX * bufferFactory.Screen.ResolutionY];
        for (int i = 0; i < Remapper.Length; i++)
        {
            Remapper[i] = i;
        }
        FisherYatesShuffle(Remapper);
        openClProgram = new(
            kernelSource: kernelSource,
            kernelName: config.KernelName,
            remapperBuffer: Remapper,
            inputBufferSize: this.screenProtocol.FullBuffer.Length);

        // Initializes the test image
        DrawRainBowTestImage(new GameTime() { TotalTime = TimeSpan.FromSeconds(config.TestImageOffset) });
        this.logger.LogInformation($"Pixel buffer for the {this.GetType().Name} is ready");

    }

    public List<PixelBuffer> Loop(GameTime time, IReadOnlyList<IGamePadDevice> gamePads)
    {
        if (config.Moving)
        {
            DrawRainBowTestImage(time);
        }
        return frame;
    }

    public void DrawRainBowTestImage(GameTime time)
    {
        const float cycleDuration = 2f;

        float elapsed = (float)time.TotalTime.TotalSeconds * config.Speed;
        float cycle = (elapsed / cycleDuration) % 1f;

        int current = ((int)(elapsed / cycleDuration)) % interesstingCoordinates.Count;
        int next = (current + 1) % interesstingCoordinates.Count;

        // Zoom
        float zoom = MathF.Sin(cycle * MathF.PI);
        zoom = EaseInOutSine(zoom);
        float timeOffset = zoom * config.ZoomMultiplier;

        // Coordinate interpolation
        float coordT;
        const float transitionStart = 0.9f;
        if (cycle < 0.9f)
        {
            // Zooming in: stay at current coordinate
            coordT = 0f;
        }
        else
        {
            // Zooming out: interpolate to next coordinate
            coordT = (cycle - transitionStart) / (1f - transitionStart);   // 0..1
            coordT = SmootherStep(coordT);
        }

        var currentCoord = interesstingCoordinates[current];
        var nextCoord = interesstingCoordinates[next];

        float x = Lerp((float)currentCoord.Item1, (float)nextCoord.Item1, coordT);
        float y = Lerp((float)currentCoord.Item2, (float)nextCoord.Item2, coordT);

        openClProgram.Run(
            fullBufferMemory.Span,
            width: bufferFactory.Screen.ResolutionX,
            height: bufferFactory.Screen.ResolutionY,
            rainbow_scale: config.RainbowScale,
            offset: timeOffset,
            x,
            y,
            Remapper);
    }

    static float SmootherStep(float t)
    {
        t = Math.Clamp(t, 0f, 1f);
        return t * t * t * (t * (t * 6 - 15) + 10);
    }

    static float EaseInOutSine(float t)
    {
        return -(MathF.Cos(MathF.PI * t) - 1f) / 2f;
    }

    static float Lerp(float a, float b, float t)
    {
        return a + (b - a) * t;
    }


    /// <summary>
    /// Do an in-place shuffle
    /// https://stackoverflow.com/questions/273313/randomize-a-listt
    /// https://en.wikipedia.org/wiki/Fisher%E2%80%93Yates_shuffle
    /// </summary>
    /// <param name="array">The array that will be shuffles</param>
    private void FisherYatesShuffle<T>(T[] array)
    {
        int n = array.Length;
        while (n > 1)
        {
            n--;
            int k = Random.Shared.Next(n + 1);
            T value = array[k];
            array[k] = array[n];
            array[n] = value;
        }
    }
}
