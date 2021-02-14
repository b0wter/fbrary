Audio Book Library
==================
This is a simple CLI tool that allows you to manage your audio book library.

Commands
========
The application is run in a style similar to `git`. There are several commands to manipulate/list your library.
All commands require you to specify the library file you want to work on. 
Use the `--libraryFile` (or `-l`) parameter before specifying any command.
Currently the following commands are supported.

Add
---
```bash
./Fbrary --libraryFile $LIBRARY_FILENAME add $PATH
```
Add a file or directory to the library.
If you specify a directory it will be recursively scanned for mp3 files and added as a single audio book.
After reading all the files you will be asked to confirm the meta data. 
If you want to skip this step you need to pass the `--noninteractive` (`-n`) argument:
```bash
./Fbrary --libraryFile $LIBRARY_FILENAME add -n $PATH
```

Remove
------
```bash
./Fbrary --libraryFile $LIBRARY_FILENAME remove $ID
```
Removes an audio book from the library. Use the `list` command to find book ids.

List
----
```bash
./Fbrary --libraryFile $LIBRARY_FILENAME list $PATTERN
```
Lists all entries in the library that contain the `$PATTERN` in either their _Artist_, _Album_, _AlbumArtist_ or _Title_ field.
The `$PATTERN` is an optional parameter. If it is skipped all books are listed.
There are the following additional parameters:

 * `--completed` - lists books that have been marked completed
 * `--notcompleted` - lists books that have not been marked completed
 * `--unrated` - lists books that do not have a rating

You can freely combine the different arguments:
```bash
./Fbrary --libraryFile $LIBRARY_FILENAME list --unrated --completed
./Fbrary --libraryFile $LIBRARY_FILENAME list "Story"
./Fbrary --libraryFile $LIBRARY_FILENAME list --notcompleted
./Fbrary --libraryFile $LIBRARY_FILENAME list "Story" --completed
```

Update
------
**This command is currently in development.**

```bash
./Fbrary --libraryFile $LIBRARY_FILENAME update $ID
```
Use the update command to edit the meta data for an existing library entry.
Use the `list` command to find `$ID`s.

Rate
----
```bash
./Fbrary --libraryFile $LIBRARY_FILENAME rate $ID
```
Set a rating for the given audio book.
Use the `list` command to find `$ID`s.

Completed
---------
```bash
./Fbrary --libraryFile $LIBRARY_FILENAME completed $ID
```
Mark an audiobook as completed.
Use the `list` command to find `$ID`s.

NotCompleted
------------
```bash
./Fbrary --libraryFile $LIBRARY_FILENAME notcompleted $ID
```
Mark an audiobook as not completed.
Use the `list` command to find `$ID`s.

Unmatched
-------
```bash
./Fbrary --libraryFile $LIBRARY_FILENAME unmatched $PATH
```
Checks whether each mp3/ogg file in the given directory (and its subdirectories) is part of an audio book in the library. Use to find files that you have newly added to your files but not yet your library.

Hints
=====
If you want to quickly add many folders without checking their meta information use this command (bash required):
```bash
find $FOLDER_TO_LOOK_INSIDE -maxdepth 1 -mindepth 1 -type d -exec ./Fbrary -l $LIBRARY_FILENAME add -n '{}' \;
```
This will add each folder as a separate item in the library.the

If you store many mp3/ogg files in a folder and want to add each file as its own audio book use this command (bash required):
```bash
find $FOLDER_TO_LOOK_INSIDE -maxdepth 1 -name "*.mp3" -exec ./Fbrary -l $LIBRARY_FILENAME add -n '{}' \;
```

