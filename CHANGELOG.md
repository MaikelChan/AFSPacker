# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [1.2.2] - 2020-08-28
### Changed
- Memory usage optimizations.

## [1.2.1] - 2019-11-16
### Changed
- Fixed error when extracting files that contain invalid dates. Those dates will be ignored.

## [1.2.0] - 2015-01-16
### Added
- Able to extract AFS archives with null files.
- Able to extract multiple files with the same name. They will be automatically renamed.
- Able to create AFS archives that ignore filenames and other metadata. Useful for games like Resident Evil: Code Veronica, where AFS archives don't contain any filenames, creation dates, etc.

### Changed
- Some small fixes.

## [1.1.0] - 2012-06-18
### Changed
- Fixed a crash reading AFS archives in games like Arc Rise Fantasia.

## [1.0.0]
- Initial release.