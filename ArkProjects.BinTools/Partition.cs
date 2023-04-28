namespace ArkProjects.BinTools;

public class Partition
{
    public required long BeginAddress { get; set; }
    public required long EndAddress { get; set; }
    public required string Name { get; set; }
    public string Extension { get; set; } = "bin";
    public byte PadWith { get; set; } = 0xff;
    public long Length => EndAddress - BeginAddress;
}