# Appx files for Onix Client


## Appx files are in the Releases
* Dependencies are in the repository
  * The GDK versions need the UWPDesktop variant.
  * GDK versions also need windows app runtime https://aka.ms/windowsappsdk/1.8/latest/windowsappruntimeinstall-x64.exe
* List of available versions is in the repository
* List of available versions is generated from versions.yaml, the old v1 docs are in OldV1Docs.txt

## Version List v2 (versionsv2.json)
A list of versions and how to download and install them. It is an array of version entries:
* `version` - *(string) The display version. Split on `.` to get the numeric parts.*
* `package_version` - *(string) The package version. Split on `.` to get the numeric parts.*
* `sdk` - *(boolean) Whether this version is in the SDK Versions profile.*
* `install_type` - *(string) `uwp` or `msixvc` (how the package installs (1.21.120+ is msixvc)).*
* `url` - *(object) The primary download.*
* `mirror_url` - *(object|null) The same package as the primary download, mirrored from GitHub. Null when there is no mirror.*
* `xdelta_url` - *(object|null) The xdelta3 patch file for this release, null when the release has no delta.*

Every download field is an object: `type` plus `url`.
* `type` - *(string) `direct` or `zip` (what the url gives you).*
* `url` - *(string or array of strings) One url, or the ordered parts of a split file.*

### Download types
The `type` of a download describes what its url gives you:
* `direct` - *The url is the package file itself. Download it and install it per `install_type`.*
* `zip` - *The url gives a zip that contains the package file. Download the zip and extract the package from it.*

To use a download, take every url in its `url` in order, then:
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
