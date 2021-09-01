# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [2.0.0] - 2021-09-01
### Changed
- Complete rewrite. Now it's easier to use, it's able to handle more AFS variations and can recreate them in the exact same format variation as in the original AFS archive. It doesn't require any extra input or configuration by the user. All necessary data to recreate the AFS is stored in a metadata.json file.
- AFS archives that contain files with directory data will be extracted maintaining the same folder structure.
- Now it requires .NET Core 3.1 instead of .NET Framework 4.7.2.
### Added
- New `-i` command to previsualize information about the contents of an AFS archive.
### Fixed
- Recent fixes for AFS Packer 1.2.x are in this version as well.
- Better handling of AFS archives that contain file names with invalid characters or paths.
- Better error checking to inform the user if something goes wrong.
### Removed
- Removed `list_file` and `-nf` commands. They're not necessary anymore as the program will handle automatically in which format an AFS archive needs to be created.

## [1.2.5] - 2021-08-31
### Fixed
- Fixed not being able to extract or create AFS archives with empty name entries, for games like Winback 2: Project Poseidon.

## [1.2.4] - 2020-12-15
### Fixed
- Fixed trying to extract AFS archives into non-existing directories for games like Soul Calibur 2, which store truncated paths instead of file names.

## [1.2.3] - 2020-10-31
### Fixed
- Fixed error when trying to read the attribute information of an AFS archive that contains random bytes as padding.

## [1.2.2] - 2020-08-28
### Changed
- Memory usage optimizations.

## [1.2.1] - 2019-11-16
### Fixed
- Fixed error when extracting files that contain invalid dates. Those dates will be ignored.

## [1.2.0] - 2015-01-16
### Added
- Able to extract AFS archives with null files.
- Able to extract multiple files with the same name. They will be automatically renamed.
- Able to create AFS archives that ignore filenames and other metadata. Useful for games like Resident Evil: Code Veronica, where AFS archives don't contain any filenames, creation dates, etc.

### Fixed
- Some small fixes.

## [1.1.0] - 2012-06-18
### Fixed
- Fixed a crash reading AFS archives in games like Arc Rise Fantasia.

## [1.0.0]
- Initial release.