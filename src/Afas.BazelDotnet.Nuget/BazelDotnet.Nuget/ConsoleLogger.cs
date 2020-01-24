using System;
using System.Threading.Tasks;
using NuGet.Common;

namespace Afas.BazelDotnet.Nuget
{
  internal class ConsoleLogger : LoggerBase
  {
    public override void Log(ILogMessage message)
    {
      Console.ForegroundColor = message.Level switch
      {
        LogLevel.Debug => ConsoleColor.DarkBlue,
        LogLevel.Error => ConsoleColor.Red,
        LogLevel.Information => ConsoleColor.White,
        LogLevel.Minimal => ConsoleColor.White,
        LogLevel.Verbose => ConsoleColor.Blue,
        LogLevel.Warning => ConsoleColor.Yellow,
        _ => Console.ForegroundColor
      };
      Console.WriteLine(message.Message);
      Console.ResetColor();
    }

    public override Task LogAsync(ILogMessage message)
    {
      Log(message);
      return Task.CompletedTask;
    }
  }
}