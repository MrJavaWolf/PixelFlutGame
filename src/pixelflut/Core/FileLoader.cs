namespace pixelflut.Core;

public class FileLoader(
    ILogger<FileLoader> logger,
    IHttpClientFactory httpClientFactory)
{
    public byte[] Load(string file, CancellationToken token = default)
    {
        byte[] imageBytes;
        if (file.ToLower().StartsWith("http://") || file.ToLower().StartsWith("https://"))
        {
            logger.LogInformation($"Tries to download file: {file}");
#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits
            var httpClient = httpClientFactory.CreateClient();
            var httpResponse = httpClient.GetAsync(file, token).Result; // Ugly waits for the result, should somehow be async
            logger.LogInformation($"Response status code: {httpResponse.StatusCode}");
            imageBytes = httpResponse.Content.ReadAsByteArrayAsync(token).Result;
#pragma warning restore VSTHRD002 // Avoid problematic synchronous waits
        }
        else if (File.Exists(file))
        {
            imageBytes = File.ReadAllBytes(file);
        }
        else if (File.Exists(Path.Join(Path.GetDirectoryName(Environment.ProcessPath), file)))
        {
            imageBytes = File.ReadAllBytes(Path.Join(Path.GetDirectoryName(Environment.ProcessPath), file));
        }
        else
        {
            throw new FileNotFoundException("Could not find file to display", file);
        }

        return imageBytes;
    }

    public string FullFileName(string file)
    {
        if (file.ToLower().StartsWith("http://") || file.ToLower().StartsWith("https://"))
        {
            return file;
        }
        else if (File.Exists(file))
        {
            return file;
        }
        else if (File.Exists(Path.Join(Path.GetDirectoryName(Environment.ProcessPath), file)))
        {
            return (Path.Join(Path.GetDirectoryName(Environment.ProcessPath), file));
        }
        else
        {
            throw new FileNotFoundException("Could not find file to display", file);
        }
    }
}
