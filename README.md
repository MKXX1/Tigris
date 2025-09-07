Tigris - Audio Tool for The Outlast Trials
------------------------------------------
## Description
Audio Viewer, Exporter, Modding Tool for The Outlast Trials, with parsing game files

## Usage
Download last release

Browse folder with the game

Wait for files to load

End.

## Modding

WARNING: To replace sounds .wem file must be smaller or almost equal to the original .wem file.

Browse folder with .wem files then press "Convert File To Match Size" in "Converter" Tab, if there are no errors in the logs, then press "Make Mod" and start game

Аfter "Convert File To Match Size" the converted sound files are moved to the path "utils/repak/put-ur-files-here/"

Sounds name must have an Id or Short Name, for some sounds only Id because they have the same Short Name

Example: 

"VO_Comm_VW_Female01_Hello01.wem" or with Wwise GUID "VO_Comm_VW_Female01_Hello01_768469FF.wem", or with Id "230131776.wem" or with Wwise GUID "230131776_768469FF.wem"

Сonversion is done in Wwise, with settings for:

Voices mainly: mono, frequency 44100 or 36000, Vorbis, bitrate ~25-50 kbps. 

SFX: stereo, frequency varies depending on file, Vorbis, bitrate varies depending on file

Video Tutorial later.

## Troubleshooting
if the sound does not play, try deleting the mod .pak files

If program crashes when trying to play sound and the wrong font is displayed, then move the "utils" folder to the folder with the executable file

## Licenses
[MIT](https://github.com/MKXX1/Tigris/blob/main/LICENSE)

[CUE4Parse](https://github.com/FabianFG/CUE4Parse/blob/master/LICENSE)

[Oodle.NET](https://github.com/NotOfficer/Oodle.NET/blob/master/LICENSE)

[ImGui.NET](https://github.com/mellinoe/ImGui.NET/blob/master/LICENSE)

[Veldrid](https://github.com/veldrid/veldrid/blob/master/LICENSE)

[Newtonsoft.Json](https://github.com/JamesNK/Newtonsoft.Json/blob/master/LICENSE.md)

[vgmstream](https://github.com/vgmstream/vgmstream/blob/master/COPYING)

[NAudio](https://github.com/naudio/NAudio/blob/master/license.txt)

[repak](https://github.com/trumank/repak)

[Arial-unicode](https://github.com/Thong-ihealth/arial-unicode/blob/main/LICENSE)
