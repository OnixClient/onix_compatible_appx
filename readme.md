# Appx files for Onix Client


## Appx files are in the Releases
* Dependencies are in the repository
  * The GDK versions need the UWPDesktop variant.
  * GDK versions also need windows app runtime https://aka.ms/windowsappsdk/1.8/latest/windowsappruntimeinstall-x64.exe
  * An up to date Xbox app:  ms-windows-store://downloadsandupdates and then open it once and let it do any updates it wants.
* List of available versions is in the repository
* List of available versions is generated from versions.yaml, the old v1 docs are in OldV1Docs.txt

## Version List v2 (versionsv2.json)
An object holding the version list and the dependency profiles the versions reference:
`versions` is an array of version entries:
* `version` - *(string) The display version. Split on `.` to get the numeric parts.*
* `package_version` - *(string) The package version. Split on `.` to get the numeric parts.*
* `sdk` - *(boolean) Whether this version is in the SDK Versions profile.*
* `install_type` - *(string) `uwp` or `msixvc` (how the package installs (1.21.120+ is msixvc)).*
* `dependency_profile` - *(string) The name of the entry in `dependency_profiles` this version needs.*
* `url` - *(object) The primary download.*
* `mirror_url` - *(object|null) The same package as the primary download, mirrored from GitHub. Null when there is no mirror.*
* `xdelta_url` - *(object|null) The xdelta3 patch file for this release, null when the release has no delta.*

Every download field is an object: `type` plus `url` plus the hashes of the file the url gives you.
* `type` - *(string) `direct` or `zip` (what the url gives you).*
* `url` - *(string, or array of part objects) One url, or the file's parts when the file is split. Each part object has the same shape as this one: `type`, `url`, `sha256`, `xxh3_64` describing that part.*
* `sha256` - *(string) The SHA-256 of the file the url gives you, or of that part when `url` is an array of parts. Empty string means the url is not pinned to one file (a "latest" link), skip verification.*
* `xxh3_64` - *(string) The XXH3-64 hash of the file the url gives you as 16 lowercase hex chars, same rules as `sha256`. Verify with this one first, it is much faster.*

### Download types
The `type` of a download describes what its url gives you:
* `direct` - *The url is the package file itself. Download it and install it per `install_type`.*
* `zip` - *The url gives a zip that contains the package file. Download the zip and extract the package from it.*

To use a download, take its url (or every part's url, in order, when the file is split), then:
* If `type` is `direct` and there is a single url, that url is the package file, install it per `install_type`.
* If `type` is `direct` and there are multiple urls, the parts are one file split to fit a size limit.
Concatenate the parts in order to get the package file, then install it per `install_type`.
* If `type` is `zip` and there is a single url, that url is the zip, extract the package from it and
install it per `install_type`.
* If `type` is `zip` and there are multiple urls, the parts are one zip split to fit a file size
limit. Concatenate the parts in order to get the zip, extract the package from it and install it
per `install_type`.

### Mirrors
`mirror_url` mirrors the same package as the primary `url`, so its type can differ from the primary's
(the mirror is zipped even when the primary is a direct file, or the other way around).
When the file is split, the wrapper's own hashes are empty strings and each part object carries that
part's real hashes.

### Dependency profiles (dependency_profiles)
A map of profile name to a list of dependencies that the versions referencing that profile need.
Every dependency installs machine wide and side by side, install them once and switching game
versions never removes them. A package dependency is a minimum, not a pin, so each family is
listed once at its latest x64 release and satisfies every version of Minecraft that ever
referenced it.
* `name` - *(string) The package identity of the dependency.*
* `version` - *(string) The version of the shipped file, or the minimum package version the system must have.*
* `install_type` - *(string) `appx` (install with `Add-AppxPackage -Path`), `exe` (a Win32 installer), or `ask` (no download).*
* `prompt` - *(string|null, ask only) The message to show the user; the launcher checks the installed package version itself.*
* `quiet_args` - *(array of strings|null, exe only) The command line arguments that run the exe installer without output, e.g. `["-q"]`.*
* `url` - *(object|null) The download for this dependency, same object shape as the version downloads. Null for `ask`.*

The profiles for the current version span:
* `uwp_engagement` - *1.12.0 to 1.21.51: plain VCLibs + Store Engagement (declared in every manifest in this span).*
* `uwp` - *1.21.60 to 1.21.114: plain VCLibs only (Mojang dropped Engagement from the manifest).*
* `gdk` - *1.21.120+: Xbox App and Gaming Services as `ask` checks (a fresh system can be too far behind to install msixvc), UWPDesktop VCLibs + Windows App Runtime 1.8 (the msixvc packaging universe).*

Plain VCLibs and UWPDesktop VCLibs are different package identities and coexist without conflict,
which is why one map covers all versions.
<br><br>
---

## Generating
All published files are generated from `versions.yaml` by the generator in `JsonGenerator/`. The output lists are fixed: `versions.txt`/`.json` (all versions),  sdkversions.txt`/`.json` (versions with `sdk: true`), `profiles.txt`/`.json` (pointers to those lists), and `versionsv2.json` (all versions in the v2 schema).

## Usage
To use it just copy the url.

Version List v2 JSON
```
https://raw.githubusercontent.com/OnixClient/onix_compatible_appx/main/versionsv2.json
```
