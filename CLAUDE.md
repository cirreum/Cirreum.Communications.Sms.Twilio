# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Development Commands

### Build Commands
```bash
# Restore dependencies
dotnet restore

# Build the project
dotnet build

# Build in Release mode
dotnet build --configuration Release

# Pack NuGet package
dotnet pack --configuration Release
```

### Solution Commands
```bash
# Work with the solution file
dotnet restore Cirreum.Communications.Sms.Twilio.slnx
dotnet build Cirreum.Communications.Sms.Twilio.slnx --configuration Release
dotnet pack Cirreum.Communications.Sms.Twilio.slnx --configuration Release
```

## Project Architecture

This is a .NET 10.0 class library that provides SMS communications functionality through Twilio integration. It follows the Cirreum Foundation Framework architecture patterns.

### Key Components

- **Target Framework**: .NET 10.0
- **Language Features**: Latest C# with implicit usings and nullable reference types enabled
- **Dependencies**: 
  - Twilio SDK (7.13.6)
  - libphonenumber-csharp.extensions (9.0.18)
  - Cirreum foundation libraries (Communications.Sms, ServiceProvider, Startup)

### Build System

The project uses MSBuild with a sophisticated build configuration:

- **Directory.Build.props**: Centralized build configuration with CI/CD detection
- **Build folder**: Contains modular .props files for different aspects:
  - `Common.props`: Basic project settings and defaults
  - `PackageInfo.props`: NuGet package metadata
  - `Versioning.props`: Version management
  - `Author.props`: Author information
  - `Icon.props`: Package icon configuration
  - `SourceLink.props`: Source linking for debugging

### Code Standards

- **EditorConfig**: Comprehensive code style rules enforced via `.editorconfig`
- **Namespace Convention**: Follows `Cirreum.Communications.Sms` root namespace
- **Code Style**: Tab indentation, Pascal case for public members, interfaces prefixed with 'I'
- **Documentation**: XML documentation generation enabled

### CI/CD Pipeline

- **GitHub Actions**: Automated publish workflow triggered on releases
- **Version Strategy**: Semantic versioning with automatic version extraction from Git tags
- **Package Publishing**: Automated NuGet.org publishing with OIDC authentication

### Local Development

- **Local builds**: Default to version 1.0.100-rc for Release builds
- **InternalsVisibleTo**: Configured for test projects in local development only
- **Global SDK**: Pinned to .NET 10.0.100 with latest feature rollforward

Note: This appears to be a library project template or early-stage project as no actual source code files (.cs) are present yet in the src directory.