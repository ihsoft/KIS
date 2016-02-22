#KIS - How to build a binary and make a release

##WINDOWS users

###Prerequisites
- C# runtime of version 4 or higher.
- For making releases:
  - Python 2.7 or greater.
  - Owner or collaborator permissions in [Github repository](https://github.com/KospY/KIS).
  - Onwer or maintainer permissions on [Curseforge project](http://kerbal.curseforge.com/projects/kerbal-inventory-system-kis.

###Building
- Review file `Tools\make_binary.cmd` and ensure the path to `MSBuild` is right.
- Run `Tools\make_binary.cmd` having folder `Tools` as current.
- Given there were no compile errors the new DLL file can be found in `.\KIS\Plugins\Source\bin\Release\`.

Note: If you don't want building yourself you can use the DLL from the repository. It is updated by the maintainer each time a new version is released.

###Releasing
- Review file `Tools\make_binary.cmd` and ensure the path to `MSBuild` is right.
- Review file `Tools\make_release.py` and ensure `ZIP_BINARY` points to a ZIP compatible command line executable.
- Verify that file `KIS\Plugins\Source\Properties\AssemblyInfo.cs` has correct version number. This will be the release number!
- Run `Tools\make_release.py -p` having folder `Tools` as current.
- Given there were no compile errors the new release will live in `Releases` folder.
- Update [Github repository](https://github.com/KospY/KIS) with the files updated during the release.
- Upload new package to [Github repository releases](https://github.com/KospY/KIS/releases).
- Upload new package to [Curseforge](http://kerbal.curseforge.com/projects/kerbal-inventory-system-kis/files). Once verified the package will become available for downloading.

Note: You can run `make_release.py` without parameter `-p`. In this case release folder structure will be created in folder `Release` but no archive will be prepared.
Note: As a safety measure `make_release.py` checks if the package being built is already existing, and if it does then release process aborts. When you need to override an existing package either delete it manually or pass flag `-o` to the release script.

##iOS & Linux users

...please add your suggestions for the building phase. The release phase should work as is.
