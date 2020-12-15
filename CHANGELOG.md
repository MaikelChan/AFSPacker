# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [1.2.4] - 2020-12-15
### Fixed
- Fixed trying to extract AFS files into non-existing directories for games like Soul Calibur 2, which store truncated paths instead of file names.

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