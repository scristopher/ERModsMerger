This is just a very simple fork of https://github.com/MadTekN1/ERModsMerger.

What changed?

All I have done is updated the soulsformats framework and have included the vanilla 1.16 regulation.bin file. It's possible to do this and rebuild the project in VS in less than an hour.


* I am not making any functionality changes or improvements nor will I respond to any support issues outside of the program running.
* This will work with the current ER version
* EXE files in releases, otherwise just compile with visual studio ofc



# Elden Ring Mods Manager - Merger
Simple tool to manage and merge Elden Ring mods. Work In Progress.

Can only merge regulation.bin files for now (every other files will be overwrited depending of priority order), more merging capabilities will be added in the future.

## Usage

![](https://github.com/MadTekN1/ERModsMerger/blob/main/Documentation/Images/Manager%20Demo.gif?raw=true)
- Place ERModsManager.exe wherever you want. Desktop for example.
- If the app ask you the game path at launch, just navigate to where eldenring.exe is.
- The fun part now, drag and drop your mods (can be .zip or folder) directly in the app (don't worry, the app will most likely handle it)
- Then define mods priority by dragging them up or down in the list (top is highest)
- Press Merge and wait the logs telling you the merge is done.
- Press Play & Enjoy!

# Elden Ring Mods Merger (Console App)

Console App version with merging and automation features.

## Usage
![](https://github.com/MadTekN1/ERModsMerger/blob/main/Documentation/Images/Console%20Merger%20Demo.gif?raw=true)

Highly recommended: Use [ModEngine2](https://github.com/soulsmods/ModEngine2), place ERModsMerger.exe in the same folder and edit config_eldenring.toml as follow:
```
mods = [
    { enabled = true, name = "default", path = "MergedMods" }
]
```
Launch ERModsMerger.exe and let it guide you through the process, it will self extract and create all the folders you need, it will also give you an example and explanations to make it easy.

## Troubleshooting & Solutions

- ERModsMerger.exe don't launch: Make sure [.Net 8 Desktop Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/runtime-desktop-8.0.6-windows-x64-installer) is installed on your system.

- Fail to load config or could not locate regulation bin: This likely happen when ERModsMergerConfig\\config.json is modified with invalid values / format. Here is an example of a modified config.json:

	```json
	{
	  "GamePath": "C:\\New\\path\\to the folder of\\ELDEN RING\\Game",
	  "ModsToMergeFolderPath": "ModsToMerge",
	  "MergedModsFolderPath": "MergedMods"
	}
	```
  * Respect the format presented above and dont forget to add double `\\` between each folders in paths.
  

- Vanilla game/modded regulation.bin don't load: The app might be not compatible with this regulation.bin, make sure your game/mods are up to date (working regulation version is 1.16.0)

- Game don't launch, is buggy or mods are missing: Merges can cause some troubles depending of the overwrited files, also this tool only merge internal values of regulation.bin files (for now) and overwrite fields in benefits to the highest priority order. Any other individual conflicting files (eg: emevd.dcx anibnd.dcx msb.dcx etc..) will be overwrited using the same system of priority and potentially causing more troubles.

For now it's better to use this tool to merge mods who only have conflicting regulation.bin files and no .dcx individual files conflicts.

## Automation

Run the console app with /merge argument to automatically merge mods located inside ModsToMerge to MergedMods folder, no user interaction will be asked and the console will close after the merge.﻿﻿

## Contributing

If you wish to contribute to this project, you are very welcome. A simple [code documentation](https://github.com/MadTekN1/ERModsMerger/blob/main/Documentation/CodeDoc.md) is available to help you get started. Enjoy coding :)

## Credits & Thanks
* [SoulsMods](https://github.com/soulsmods)
* [Smithbox](https://github.com/vawser/Smithbox)
* [Nordgaren](https://github.com/Nordgaren)
* Also big Thanks to all the Souls [modding community](https://discord.gg/servername)
