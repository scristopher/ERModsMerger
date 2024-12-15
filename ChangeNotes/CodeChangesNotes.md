# Changes for 1.3.x-alpha

## ERModsManager
<details>
<summary><h3>1.3.0-alpha</h3></summary>
<br>

- [x] Creation of the visual app
- [x] File(s) Drag & Drop
- [x] Arrange mod priority by dragging them up or down
- [x] Enable/Disable/Delete mod(s) via checkboxes and buttons
- [x] First launch scenario: extract embedded files to app data folder, ask user game path if not found
- [x] Config:
  - [x] Re-set game path (file dialog)
  - [x] Open local app data folder
- [x] Help & Credits
- [x] Save actives mods & arrangements (priorities) in config

</details>

<details>
<summary><h3>1.3.1-alpha</h3></summary>
<br>

- [x] Config:
- [x] Enable / Disable all mods
- [x] Expandable mods
  - [x] Add / modify note
  - [x] Show files / folders tree
    - [x] Enable / Disable particular file / folder
	- [x] Save in current config
	- [x] Highlight red / orange when file conflict is found (red = not supported and will be overwritten, orange = supported and internal merge will occur)
- [x] Profiles
  - [x] Dropdown list for loading or add new merge / config profiles
  - [x] Inform user when he try to launch the game before the selected profile is ready (not merged)
- [x] Re-Merge using profile(s) merged mods
- [x] Better logs
  - [x] Logs presented in a list instead of textblock
  - [x] Colorized icons for different log types
  - [x] Progress bar log type
  - [x] Expandable logs (logs groups per merging file type)
- [x] Add reset button in config tab
- [x] Direct launch of ModEngine2 with absolute paths in args
  


#### Fixes

- [x] Crash when local files / folders are modified / deleted
  - [x] Check files / folders at app launch (also before edits events) and update concerned config / profile accordingly
- [x] Fixed a startup error: object reference not set to an instance of an object 
</details>

<details open>
<summary><h3>1.3.x-alpha</h3></summary>
<br>

- [x] Config:
  - [ ] Custom location for ModsToMerge and MergedMods
  
</details>


## ERModsMerger.Core

<details>
<summary><h3>1.3.0-alpha</h3></summary>
<br>

- [x] Searches of unsuported conflicts + ask user confirmation to continue
- [x] Initial support for the merge of .msgbnd.dcx files
- [x] Partial support for the merge of .emevd.dcx files
- [x] Better implementation for logs and user / console queries
- [x] Read content packed files in game folder instead of unpacking everything by using [BHD5Reader, thanks to Nordgaren](https://github.com/Nordgaren/ERBingoRandomizer/blob/main/src/ERBingoRandomizer/FileHandler/BHD5Reader.cs)
- [x] handle oodle location in SoulsFormats depending of scenarios (console / WPF app)
- [x] UnPack/UnZip embedded resources to AppData
  - [x] Regulations
  - [x] Dictionaries
  - [x] ParamDefs
  - [x] Pre-configured modengine as embedded resource
  
</details>

<details>
<summary><h3>1.3.1-alpha</h3></summary>
<br>

- [x] Better implementation for logs and user / console queries
  - [x] Logs grouping
- [x] Check files / folders ignored by user in config
- [x] Detect & Handle .dll mods, editing modengine config_eldenring.toml accordingly
  - [x] ini integration
- [x] (UnZip mods and) search in arborescences, align to correct paths using [EldenRingDictonnary.txt, thanks to Nordgaren again](https://github.com/Nordgaren/UXM-Selective-Unpack/blob/master/UXM/res/EldenRingDictionary.txt)
  - [x] Ignore files that are not present in dictionary (readme, json project files, etc)
- [x] Better implementation for regulation merging
  - [x] When all files are loaded, apply modifications


#### Fixes

- [x] Vanilla dcx not loading
- [x] Crash at save game load when for certain .msgbnd.dcx files
- [x] Merging .prev files into reg bin

</details>

<details open>
<summary><h3>1.3.x-alpha</h3></summary>
<br>


- [ ] Internal events instructions conflicts merges
- [x] (UnZip mods and) search in arborescences, align to correct paths using [EldenRingDictonnary.txt, thanks to Nordgaren again](https://github.com/Nordgaren/UXM-Selective-Unpack/blob/master/UXM/res/EldenRingDictionary.txt)
  - [ ] Ask user when multiple versions (duplicates) is found
- [ ] .csv merge support
- [ ] Upgrade old regulation bin files using stored reg bin vanilla file of the same version
- [x] Better implementation for regulation merging
  - [ ] Manual conflict resolving for the new regulation merge


</details>