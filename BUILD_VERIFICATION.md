# Build Verification and Troubleshooting

## Common Compilation Errors and Solutions

### CS8802: Only one compilation unit can have top-level statements

**Cause**: Multiple files contain top-level statements or mixing top-level statements with Main methods.

**Prevention**: 
- Only `ERModsMerger/Program.cs` uses top-level statements
- Do NOT add `static void Main()` methods to any files in the ERModsMerger project
- Do NOT create additional files with top-level statements

**Solution**: 
- Remove any additional Main methods
- Convert any additional top-level statement files to use classes/methods

### CS0436: Type conflicts with SoulsFormats

**Cause**: Both SoulsFormats project reference AND NuGet package reference exist.

**Prevention**:
- ERModsMerger.Core references the embedded SoulsFormats project
- Do NOT add SoulsFormats as a PackageReference to any project
- Comments in .csproj files warn against this

**Solution**:
- Remove any SoulsFormats PackageReference entries
- Only use the ProjectReference to the embedded SoulsFormats source

## Project Reference Structure

```
ERModsMerger (console app, top-level statements)
├── ERModsMerger.Core (library)
    ├── SoulsFormats (embedded source)

ERModsManager (WPF app)
├── ERModsMerger.Core (library)
    ├── SoulsFormats (embedded source)
```

## Build Requirements

- .NET 8.0 Windows (net8.0-windows)
- Windows Desktop SDK (for WPF components)
- Visual Studio or .NET SDK with Windows desktop workload

## Verification Steps

1. Build solution: `dotnet build ERModsMerger.sln`
2. Check for CS8802 errors: No multiple entry points
3. Check for CS0436 errors: No duplicate type references
4. Verify Program.cs uses top-level statements only
5. Verify no SoulsFormats PackageReference exists