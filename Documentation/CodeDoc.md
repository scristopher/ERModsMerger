# Code Documentation

If you are here, you may be a developer and want to have a better understanding of the underlying structure or even wish to contribute to this awesome project.

This documentation is here to broadly define how this tool works to help you get started.

## Structure

This project is divided into 3 essential parts:

- `ERModsMerger.Core` is the "core" of this project, contains all the main classes and functions needed for merge.
- `ERModsManager` is the WPF UI App
- `ERModsMerger` is the Console App


## ERModsMerger.Core

How does the merge take place in the algo:
  
### `ModsMerger` class

Main entry point of the merger Core and used with a simple call of `StartMerge()` function:
  - Retrieve the `ModsMergerConfig` (merge config class) precedently loaded in Console / UI App.
  - Foreach mod directories => Find all files.
  - Add all files in `MergeableFilesDispatcher` class, used to search for conflicts and merge all.
  
<br>

### `MergeableFilesDispatcher` class

As its name suggests, this class determines what type the files are and where to send them for merge.
Also include functions to group files to merge by their name and types respecting the configured priority order set in config.

Methods:

- `AddFile(string path)` Add a file to the dispatcher by storing them in a list of `FileToMerge` object.
- `SearchForConflicts()` Search and group all individual mod file conflicts and store them in a list of `FileConflict` object.
- `MergeAllConflicts()` Depending of their format, send all `FileConflict` to its respective `Formats.*` merger class by calling `Formats.*.MergeFiles(List<FileToMerge> files)` static method.

<br>

### `Formats.*` classes

We can find in this folder / namespace all the merger classes for each file format (eg: regulation.bin or dcx files) and called by the `MergeableFilesDispatcher`

Methods:

- `contructor(string path)` Initialize and load file using `SoulsFormats` lib.
- `Save(string path)` Save the modified file to specified location.
- `static MergeFiles(List<FileToMerge> files)` Load all files and apply merge logic then save the modified file to configured directory.

<br>

## ERModsMerger (Console App)

The console app is pretty straightforward when it comes to merging, everything is in `program.cs` as almost everything is called from the core lib.

## ERModsManager (WPF UI App)

WPF is a little more complex as it implement much more possibilities and user friendly behaviors. But things are splitted using various UsersControls.

Main views / tab are:

- `CustomWindow` Main window used to initialize differents tabs / user controls, also contain first launch scenario and config loading behaviors.
- `ModsListUC` UserControl listing mods, include drag & drop mechanics loading / creating `ModsItemUC` mods into a StackPanel
- `LogsUC` UserControl displaying logs from the Core.
- `ConfigUC` UserControl to modify loaded config.