# BioChip

BioChip is a customized version of [PalmSens PSExampleApp](https://github.com/PalmSens/PSExampleApp), extended with additional functionalities for biochip measurements and automated electrochemical workflows.

## Features
- Integration with PalmSens devices
- Automated measurement sequences
- Extended parameter handling (incubation time, repeat numbers, repeat intervals, etc.)
- Improved data logging and sample naming
- User-friendly interface for electrochemical analysis

## Requirements
- Visual Studio 2022 or later
- .NET Framework (specify version, e.g., .NET 6.0 or .NET Framework 4.8)
- PalmSens SDK (if required)

## Installation
1. Clone this repository:
   ```bash
   git clone https://github.com/mkh23/BioChip.git
   cd BioChip
2. Open in Visual Studio
   2.1. Launch Visual Studio 2022 (or later).
   2.2. Go to File > Open > Project/Solution.
   2.3. Select the file PSExampleApp.sln.

3. Restore Dependencies
   - Visual Studio should automatically restore required NuGet packages when the solution is opened.
   - If not, right-click the solution in Solution Explorer → choose Restore NuGet Packages.

4. Build the Project
   - In Visual Studio, select Build > Build Solution (or press Ctrl+Shift+B).
   - Ensure the build completes without errors.

5. Run the Application
   - Press F5 to run with debugging, or Ctrl+F5 to run without debugging.
   - The BioChip interface will launch and be ready to use.

6. Connect a Device (Optional)
   - Connect a PalmSens device via USB or Bluetooth.

Make sure the appropriate PalmSens SDK/drivers are installed.

The app should automatically detect the device and allow you to start measurements.
