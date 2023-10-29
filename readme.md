# Appx files for Onix Client


## Appx files are in the Releases
* Dependencies are in the repository
* List of available versions is in the repository

## Definitions
### A profile is a group of version, It contains the following
*Order matches .txt, step is 3*.
* Display Name - *Nice name for users*.
* File Name - *How the file for it is named*.
* Url - *Url of the profile version list*.

<br>

---
### A version list contains information about versions
*Order matches .txt, step is 3*.

* Display Version - *What will show in the main menu*.
* Package Version - *The package version*.
* Url - *The url to download the appx*.


## JSON
### Json data is generated from the .txt automatically
### Profile
* `name` - *(string) The display name for users.*
* `file_name` - *(string) The name of the profile file.*
* `url` - *(string) The url to the profile json.*

---
### Version Number
* `major` - *(integer) The major version number.*
* `minor` - *(integer) The minor version number.*
* `patch` - *(integer) The patch version number.*
* `build` - *(integer) The build version number.*
---
### Version List
* `version` - *(string) The display version.*
* `package_version` - *(string) The package version.*
* `version_number` - *(Version Number)  The version as `Version Number`.*
* `package_version_number` - *(Version Number) The package. version as `Version Number`.*
* `url` - *(string) The url to the appx.*
<br><br>
---

## Usage
To use it just select the format you want to use and copy the url.

TXT
```
https://raw.githubusercontent.com/bernarddesfosse/onix_compatible_appx/main/profiles.txt
```
JSON
```
https://raw.githubusercontent.com/bernarddesfosse/onix_compatible_appx/main/profiles.json
```
