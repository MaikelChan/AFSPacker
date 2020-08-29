# AFS Packer
AFS Packer can extract the files inside an AFS archive to a folder, or generate a new AFS archive with the files located in a folder. The AFS format is used in many games from companies like Sega.

## Requirements
The program requires the .NET Framework 4.7.2.

## Usage
```
AFSPacker -e <input_file> <output_dir> [list_file]        :  Extract AFS archive
AFSPacker -c <input_dir> <output_file> [list_file] [-nf]  :  Create AFS archive

    list_file: will create or read a text file containing a list of all the
               files that will be extracted/imported from/to the AFS archive.
               This is useful if you need the files to be in the same
               order as in the original AFS (required for Shenmue 1 & 2).

          -nf: will create the AFS archive with no filenames. This is useful for
               some games like Resident Evil: Code Veronica, that have AFS
               archives with files that don't preserve their file names,
               creation dates, etc.
```