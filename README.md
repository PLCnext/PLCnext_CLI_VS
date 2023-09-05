# Welcome to the PLCnCLI Visual Studio plugin repository

This repository is part of the PLCnext Toolchain and provides a Visual Studio IDE extension for programming for PLCnext Technology in high level language. The PLCnext CLI provides the entire toolchain for programming on the PLCnext Technology platform.

The main repository for the CLI can be found [here](https://github.com/PLCnext/PLCnext_CLI).

## Installation & First Steps

If you want the latest release version of the PLCnext CLI, you can find it in the [PLCnext toolchain product page](https://www.phoenixcontact.com/qr/1639782).<br/>
For more information and first steps with PLCnext Technology please visit our [PLCnext Community](https://www.plcnext-community.net/infocenter/programming/plcnext-programming_introduction).

## Test a local build

The following steps describe how to build a fully functional PLCnCLI visual studio plugin locally on your machine to test the newest version or to test your own changes.

### Prerequisite

- [Visual Studio 2019 with installed Visual Studio Extension Development package](https://visualstudio.microsoft.com/de/vs/community/ "Visual Studio 2019 with installed Visual Studio Extension Development package")

### Build steps

- `MSBuild.exe /t:Restore;Build src\PlcNextVSExtension.sln /p:LangVersion=7.3 /p:DeployExtension=false`
- Plugin can be found in `src\PlcNextVSExtension\bin\Release\PlcNextVSExtension.vsix`

## Contribute

You can participate in this project by submitting bugs and feature requests.<br/>
Furthermore you can help us by discussing issues and let us know where you have problems or where others could struggle.

## Feedback

You can give feedback to this project in different ways:

- Ask a question in our [PLCnext Community Forum](https://www.plcnext-community.net/forum).
- File a bug or request a new feature in [GitHub Issues](https://github.com/PLCnext/PLCnext_CLI_VS/issues).

## License

Copyright (c) Phoenix Contact GmbH & Co KG. All rights reserved.<br/>

Licensed under the [Apache 2.0](LICENSE) License.