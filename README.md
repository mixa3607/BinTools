# BinTools
![license](https://img.shields.io/github/license/mixa3607/BinTools?style=flat-square)
![workflow](https://img.shields.io/github/actions/workflow/status/mixa3607/BinTools/push.yml?style=flat-square)
![latest release](https://img.shields.io/github/v/release/mixa3607/BinTools?style=flat-square)

# Download
Latest build in [Actions](./actions)

Release in [Releases](./release)

# HowTo

## parts.json format
```json
{
  "Partitions": [
    {
      "BeginAddress": 0, "EndAddress": 0x000000040000,
      "Name": "boot", "Extension": "bin", "PadWith": 0x55
    },
    {
      "BeginAddress": 0x000000040000, "EndAddress": 2293760,
      "Name": "kernel", "Extension": "uimg"
    }
  ]
}
```
Partition members:
- BeginAddress (required)
- EndAddress (required)
- Name (required)
- Extension (default "bin")
- PadWith (default 0xff)


## Commands
```shell
$ ./bintools adjust --help
Description:
  Strip or pad file to target size

Usage:
  bintools adjust [options]

Options:
  -i, --in <in>      Source file
  -o, --out <out>    Output file
  --pad <pad>        Byte for padding [default: 255]
  -s, --size <size>  Target size
  --overwrite        Overwrite existed file [default: True]
  -?, -h, --help     Show help and usage information

$ ./bintools adjust -i ./100bytes_file.bin -o ./200bytes_file.bin -s 200 --pad 0xFF  #pad
$ ./bintools adjust -i ./200bytes_file.bin -o ./100bytes_file.bin -s 100             #cut
```

```shell
$ ./bintools split --help
Description:
  Split file to partitions

Usage:
  bintools split [options]

Options:
  -p, --parts <parts>  Json file with defined partitions [default: ./parts.json]
  -i, --in <in>        Source file
  -o, --out <out>      Output directory
  -n, --names <names>  Specific part names. Leave empty for all []
  --overwrite          Overwrite existed files [default: True]
  -?, -h, --help       Show help and usage information

$ ./bintools split -p ./test.json -i ./bytes.bin -o ./out_dir -n boot -n para  #extract boot and para partitions
$ ./bintools split -p ./test.json -i ./bytes.bin -o ./out_dir                  #extract all partitions
```

```shell
$ ./bintools concat --help
Description:
  Concat partitions to file

Usage:
  bintools concat [options]

Options:
  -p, --parts <parts>  Json file with defined partitions [default: ./parts.json]
  -i, --in <in>        Source directory
  -o, --out <out>      Output file
  -n, --names <names>  Specific part names. Leave empty for all []
  --overwrite          Overwrite existed file [default: True]
  -?, -h, --help       Show help and usage information
  
$ ./bintools concat -p ./test.json -i ./in_dir -o ./bytes.bin -n boot -n para  #concat boot and para partitions
$ ./bintools concat -p ./test.json -i ./in_dir -o ./bytes.bin                  #concat all partitions
```