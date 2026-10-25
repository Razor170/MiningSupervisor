namespace MiningSupervisor.Models;

public class LogFileCursor
{
    public string ListenerName { get; set; } = "";
    public long ByteOffset { get; set; } = 0;
}
