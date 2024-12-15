# Patch Note 1.3.1-alpha

## What's new

- Mods file tree & preview conflicting files
- dll mods support
- Profiles
- Better logs

Full details in CodeChangesNotes

## Bug fixes
- Crash when local files / folders are modified / deleted
  - Check files / folders at app launch (also before edits events) and update concerned config / profile accordingly
- Fixed a startup error: object reference not set to an instance of an object
- Vanilla dcx not loading
- Crash at save game load when for certain .msgbnd.dcx files
- Merging .prev files into reg bin