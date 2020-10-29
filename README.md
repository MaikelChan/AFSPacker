# AFS Packer
AFS Packer can extract the files inside an AFS archive to a folder, or generate a new AFS archive with the files located in a folder. The AFS format is used in many games from companies like Sega.

## Requirements
The program requires the [.NET Core Runtime 3.1](https://dotnet.microsoft.com/download/dotnet-core/3.1) and it works on Windows, Linux and MacOS.

## Usage
```
AFSPacker -e <input_afs_file> <output_dir>  :  Extract AFS archive
AFSPacker -c <input_dir> <output_afs_file>  :  Create AFS archive
```