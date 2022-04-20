using System;
using System.Linq;
using System.Text;
using System.Threading;
using DevDecoder.HIDDevices;
using DevDecoder.HIDDevices.Usages;
using DevDecoder.HIDDevices.Controllers;
using Microsoft.Extensions.Logging;
using DynamicData;

public class GameLoopSample : Sample
{
    /// <inheritdoc />
    public override string Description =>
        "Demonstrates a classic synchronous game loop.";

    /// <inheritdoc />
    protected override void Execute()
    {
        // Create a singleton instance of the controllers object, that we should dispose
        // on closing the game, here we use a using block, but can obviously call controllers.Dispose()
        using var devices = new Devices(CreateLogger<Devices>());

        Logger.LogInformation("Press A Button to exit!");

        // Holds a reference to the current gamepad, which is set asynchronously as they are detected.
        Gamepad? gamepad = null;
        var batch = 0;

        var subscription2 =
            devices.Connected(
            device => device.ControlUsagesAll(GenericDesktopPage.X, GenericDesktopPage.Y))
            .Subscribe(devices =>
            {
                foreach (var device in devices)
                {
                    device.Current.Subscribe(changes =>
                    {
                        foreach (var change in changes)
                        {
                            foreach (var usage in change.Control.Usages)
                            {
                                if (usage.Types.Count() == 0) continue;
                                if (change.Value > 0.45 && change.Value < 0.55)
                                {
                                    continue;
                                }

                                Logger.LogInformation($"" +
                                    $"Timestamp: {change.Timestamp}, " +
                                    $"Control: {change.Control}, " +
                                    $"Value: {change.Value}, " +
                                    $"Elapsed: {change.Elapsed}, " +
                                    $"Device: {device.Current.Name}, " +
                                    $"");


                            }
                        }

                    });
                }

            });


        Console.ReadLine();

        //        // Controller to any gamepads as they are found
        //        using var subscription = devices.Controllers<Gamepad>().Subscribe(g =>
        //{


        //    // If we already have a connected gamepad ignore any more.
        //    // ReSharper disable once AccessToDisposedClosure
        //    Logger.LogInformation("New device! Go go go");
        //    if (gamepad?.IsConnected == true)
        //    {
        //        return;
        //    }

        //    // Assign this gamepad and connect to it.
        //    gamepad = g;
        //    g.Connect();
        //    batch = 0;
        //    Logger.LogInformation($"{gamepad.Name} found!  Following controls were mapped:");
        //    foreach (var (control, infos) in g.Mapping)
        //    {
        //        Logger.LogInformation(
        //            $"  {control.Name} => {string.Join(", ", infos.Select(info => info.PropertyName))}");
        //    }
        //});

        //        var timestamp = 0L;
        //        try
        //        {
        //            // Our 'game loop'
        //            while (true)
        //            {
        //                // Sleep to simulate a game loop.
        //                Thread.Sleep(15);

        //                // If we haven't got a gamepad, or the current one isn't connected, wait for a connected gamepad.
        //                var currentGamepad = gamepad;
        //                if (currentGamepad?.IsConnected != true) { continue; }

        //                // Look for any changes since the last detected change.
        //                var changes = currentGamepad.ChangesSince(timestamp);
        //                if (changes.Count > 0)
        //                {
        //                    var logBuilder = new StringBuilder();

        //                    logBuilder.Append("Batch ").Append(++batch).AppendLine();
        //                    foreach (var value in changes)
        //                    {
        //                        // We should update our timestamp to the last change we see.
        //                        if (timestamp < value.Timestamp)
        //                        {
        //                            timestamp = value.Timestamp;
        //                        }

        //                        var valueStr = value.Value switch
        //                        {
        //                            bool b => b ? "Pressed" : "Not Pressed",
        //                            double d => d.ToString("F3"),
        //                            null => "<null>",
        //                            _ => value.Value.ToString()
        //                        };
        //                        logBuilder.Append("  ")
        //                            .Append(value.PropertyName)
        //                            .Append(": ")
        //                            .Append(valueStr)
        //                            .Append(" (")
        //                            .AppendFormat("{0:F3}", value.Elapsed.TotalMilliseconds).AppendLine("ms)");
        //                    }

        //                    Logger.LogInformation(logBuilder.ToString());
        //                }

        //                // Or directly access controls
        //                //if (currentGamepad.AButton)
        //                //{
        //                //    Logger.LogInformation("A Button pressed, finishing.");
        //                //    return;
        //                //}
        //            }
        //        }
        //        finally
        //        {
        //            // Ensure gamepad connection is disposed to stop listening to the gamepad
        //            if (gamepad != null)
        //            {
        //                gamepad.Dispose();
        //                Logger.LogInformation($"{gamepad.Device.Name} disconnected!");
        //            }
        //        }
    }
}