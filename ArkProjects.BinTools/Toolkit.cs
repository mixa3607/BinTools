using Microsoft.Extensions.Logging;

namespace ArkProjects.BinTools;

public class Toolkit
{
    public IReadOnlyList<Partition> Partitions { get; }
    private readonly ILogger<Toolkit> _logger;

    public Toolkit(IReadOnlyList<Partition> partitions, ILogger<Toolkit> logger)
    {
        Partitions = partitions;
        _logger = logger;
    }

    public async Task AdjustSizeAsync(string inFile, string outFile, long targetSize, byte padWith,
        bool overwrite = true)
    {
        if (!File.Exists(inFile))
        {
            _logger.LogError("File {f} not exist", inFile);
            throw new FileNotFoundException($"File {inFile} not found");
        }

        if (File.Exists(outFile))
        {
            if (overwrite)
            {
                _logger.LogWarning("File {f} exist. Overwrite", outFile);
                File.Delete(outFile);
            }
            else
            {
                _logger.LogWarning("File {f} exist", outFile);
                return;
            }
        }

        await using var inFileStream = File.OpenRead(inFile);
        await using var outFileStream = File.OpenWrite(outFile);
        if (targetSize == inFileStream.Length)
        {
            _logger.LogInformation("Target size {t} and source is same. Copy", targetSize);
            await inFileStream.CopyToAsync(outFileStream);
        }
        else if (targetSize > inFileStream.Length)
        {
            var pad = targetSize - inFileStream.Length;
            _logger.LogInformation("Target size {t} more then source. Pad {p} with 0x{w:X2} bytes", targetSize, pad,
                padWith);
            await inFileStream.CopyToAsync(outFileStream);
            for (int i = 0; i < pad; i++)
                outFileStream.WriteByte(padWith);
        }
        else if (targetSize < inFileStream.Length)
        {
            var strip = inFileStream.Length - targetSize;
            _logger.LogInformation("Target size {t} less then source. Strip {p} bytes", targetSize, strip);
            for (int i = 0; i < targetSize; i++)
            {
                var b = (byte)inFileStream.ReadByte();
                outFileStream.WriteByte(b);
            }
        }
    }

    public async Task ConcatPartsToBin(string inDir, string outFile, IReadOnlyList<string>? partNames = default,
        bool overwrite = true)
    {
        if (!Directory.Exists(inDir))
        {
            _logger.LogInformation("Directory {d} not exist", inDir);
            throw new DirectoryNotFoundException($"Dir {inDir} not found");
        }

        if (File.Exists(outFile))
        {
            if (overwrite)
            {
                _logger.LogWarning("File {f} exist. Overwrite", outFile);
                File.Delete(outFile);
            }
            else
            {
                _logger.LogError("File {f} exist. Abort", outFile);
                return;
            }
        }

        partNames = partNames?.Distinct().ToArray() ?? Array.Empty<string>();
        if (partNames.Count == 0)
        {
            _logger.LogDebug("Part names not specified, export all");
            partNames = Partitions.Select(x => x.Name).ToArray();
        }

        var dstBinBytes = new byte[Partitions.Max(x => x.EndAddress)];
        var minAddr = long.MaxValue;
        var maxAddr = long.MinValue;
        foreach (var partName in partNames)
        {
            var partDef =
                Partitions.FirstOrDefault(x => x.Name.Equals(partName, StringComparison.InvariantCultureIgnoreCase));
            if (partDef == null)
            {
                _logger.LogWarning("Part {n} not defined! Skip.", partName);
                continue;
            }

            var srcFile = Path.Combine(inDir, $"{partDef.Name}.{partDef.Extension}");
            await using var srcFileStream = File.OpenRead(srcFile);
            if (srcFileStream.Length > partDef.Length)
            {
                _logger.LogError("File {f} ({fl}) length more than allowed in section {d} ({ds})", srcFile,
                    srcFileStream.Length, partDef.Name, partDef.Length);
                continue;
            }

            var read = await srcFileStream.ReadAsync(dstBinBytes, (int)partDef.BeginAddress, (int)srcFileStream.Length);
            var pad = partDef.Length - read;
            for (int i = 0; i < pad; i++)
            {
                dstBinBytes[i + partDef.EndAddress] = partDef.PadWith;
            }

            if (partDef.BeginAddress < minAddr)
                minAddr = partDef.BeginAddress;
            if (partDef.EndAddress > maxAddr)
                maxAddr = partDef.EndAddress;

            _logger.LogInformation("Write {b1} bytes, Pad {b2} bytes", read, pad);
        }

        await using var outFileStream = File.OpenWrite(outFile);
        await outFileStream.WriteAsync(dstBinBytes, (int)minAddr, (int)(maxAddr - minAddr));
        outFileStream.Flush();
        _logger.LogInformation("Write range 0x{b:X8}-0x{e:X8} to {f}", minAddr, maxAddr, outFile);
    }

    public async Task SplitBinToPartsAsync(string inFile, string outDir, IReadOnlyList<string>? partNames = default,
        bool overwrite = true)
    {
        if (!File.Exists(inFile))
        {
            _logger.LogError("File {f} not exist", inFile);
            throw new FileNotFoundException($"File {inFile} not found");
        }

        if (!Directory.Exists(outDir))
        {
            _logger.LogInformation("Directory {d} not exist. Create", outDir);
            Directory.CreateDirectory(outDir);
        }

        partNames = partNames?.Distinct().ToArray() ?? Array.Empty<string>();
        if (partNames.Count == 0)
        {
            _logger.LogDebug("Part names not specified, export all");
            partNames = Partitions.Select(x => x.Name).ToArray();
        }

        var srcBinBytes = await File.ReadAllBytesAsync(inFile);

        foreach (var partName in partNames)
        {
            var partDef =
                Partitions.FirstOrDefault(x => x.Name.Equals(partName, StringComparison.InvariantCultureIgnoreCase));
            if (partDef == null)
            {
                _logger.LogWarning("Part {n} not defined! Skip.", partName);
                continue;
            }

            _logger.LogInformation("Process part {n}. Range 0x{b:X8}-0x{e:X8}, len {l}", partDef.Name,
                partDef.BeginAddress,
                partDef.EndAddress, partDef.Length);
            var dstPath = Path.Combine(outDir, $"{partDef.Name}.{partDef.Extension}");
            if (File.Exists(dstPath))
            {
                if (overwrite)
                {
                    _logger.LogWarning("File {f} exist. Overwrite", dstPath);
                    File.Delete(dstPath);
                }
                else
                {
                    _logger.LogWarning("File {f} exist. Skip", dstPath);
                    continue;
                }
            }

            await using var dstBinStream = File.OpenWrite(dstPath);
            dstBinStream.Write(srcBinBytes, (int)partDef.BeginAddress, (int)partDef.Length);
            dstBinStream.Flush();
            _logger.LogInformation("Write {n} bytes to {d}", dstBinStream.Length, dstPath);
        }
    }
}