#KIS - How to build a binary and make a release

##WINDOWS users

###Prerequisites
- For building:
  - Get C# runtime of version 4.0 or higher.
  - Create a virtual drive pointing to KSP installation: `subst q: <path to KSP root>`. I.e. if `KSP.exe` lives in `S:\Steam\Kerbal Space Program\` then this is the root.
    - If you choose not to do that or the drive letter is different then you also need to change `KIS.csproj` project file to correct references and post-build actions.
- For making releases:
  - Python 2.7 or greater.
  - Owner or collaborator permissions in [Github repository](https://github.com/KospY/KIS).
  - Owner or maintainer permissions on [Curseforge project](http://kerbal.curseforge.com/projects/kerbal-inventory-system-kis).
- For development:
  - Install an open source [SharpDevelop](https://en.wikipedia.org/wiki/SharpDevelop). It will pickup existing project settings just fine but at the same time can add some new changes. Please, don't submit them into the trunk until they are really needed to build the project.
  - Get a free copy of [Visual Studio Express](https://www.visualstudio.com/en-US/products/visual-studio-express-vs). It should work but was not tested.

###Versioning
Version number consists of three numbers - X.Y.Z:
- X - MAJOR. It a really huge change to the product that may make a new vision to it. Like releasing a first version: it's always a huge change.
- Y - MINOR. Adding a new functionality or removing an old one (highly discouraged) is that kind of changes.
- Z - PATCH. Bugfixes, small feature requests, and internal cleanup changes.

###Building
- Review file `Tools\make_binary.cmd` and ensure the path to `MSBuild` is right.
- Run `Tools\make_binary.cmd` having folder `Tools` as current.
- Given there were no compile errors the new DLL file can be found in `.\KIS\Plugins\Source\bin\Release\`.

###Releasing
- Review file `Tools\make_binary.cmd` and ensure the path to `MSBuild` is right.
- Review file `Tools\make_release.py` and ensure `ZIP_BINARY` points to a ZIP compatible command line executable.
- Verify that file `KIS\Source\Properties\AssemblyInfo.cs` has correct version number. This will be the release number!
- Go thru `KIS\CHANGELOG.md`:
  - Ensure all Github tracked issues are reflected via "[Fix #NNN] <description>" records.
  - Ensure all implemented echancements and/or changes are reflected via "[Enhancement]" or "[Change]" tags.
- Update version number in [AssemblyInfo.cs](https://github.com/KospY/KIS/blob/master/Plugins/Source/Properties/AssemblyInfo.cs).
- Run `Tools\make_release.py -p` having folder `Tools` as current.
- Given there were no compile errors the new release will live in `Releases` folder and a release archive is created in the project's root.
- Upload new package to [Curseforge](http://kerbal.curseforge.com/projects/kerbal-inventory-system-kis/files). Wait till new package is verified.
- Update [netkan file](https://github.com/KospY/KIS/blob/master/KIS.netkan) with the newly uploaded file.
- Commit release changes and update remote  [Github repository](https://github.com/KospY/KIS) with the files updated during the release.
- Make a [Github release](https://github.com/KospY/KIS/releases). Do *not* attach release binary to it. Use changes from `CHANGELOG.md` as a release description.

_Note_: You can run `make_release.py` without parameter `-p`. In this case release folder structure will be created in folder `Release` but no archive will be prepared.

_Note_: As a safety measure `make_release.py` checks if the package being built is already existing, and if it does then release process aborts. When you need to override an existing package either delete it manually or pass flag `-o` to the release script.

##iOS & Linux users

...please add your suggestions for the building phase. The release phase should work as is.
