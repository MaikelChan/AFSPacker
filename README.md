# AFS Packer

![GitHub release (latest SemVer)](https://img.shields.io/github/v/release/MaikelChan/AFSPacker?sort=semver&style=for-the-badge)
![GitHub commits since latest release (by SemVer)](https://img.shields.io/github/commits-since/MaikelChan/AFSPacker/latest?color=orange&sort=semver&style=for-the-badge)
![GitHub all releases](https://img.shields.io/github/downloads/MaikelChan/AFSPacker/total?color=yellow&style=for-the-badge)
![GitHub](https://img.shields.io/github/license/MaikelChan/AFSPacker?style=for-the-badge)

AFS Packer can extract the files inside an AFS archive to a folder, or generate a new AFS archive from the files located in a folder. The AFS format is used in many games from companies like Sega. Even though it's a simple format, there are lots of quirks and edge cases in many games that require implementing specific fixes or workarounds for the program to work with them. If you encounter any issue with a specific game, don't hesitate to report it.

AFS Packer is powered by [AFSLib](https://github.com/MaikelChan/AFSLib), a library that can extract, create and manipulate AFS files.

## Requirements
The program requires the [.NET Runtime 6](https://dotnet.microsoft.com/download/dotnet/6.0) and it works on Windows, Linux and MacOS.

## Changelog
You can check the changelog [here](https://github.com/MaikelChan/AFSPacker/blob/v2/CHANGELOG.md).

## Usage
```
AFSPacker -e <input_afs_file> <output_dir>  :  Extract AFS archive
AFSPacker -c <input_dir> <output_afs_file>  :  Create AFS archive
AFSPacker -i <input_afs_file>               :  Show AFS information
```