![logo](https://raw.githubusercontent.com/b0wter/fbrary/master/logo.png)

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
 * `--ids $ID1 $ID2 $..` - list only books with the given idst
 * `--format $FORMAT_STRING` - configure how to display the results (see below)
 * `--table $FORMAT_STRING` - display results as table with the given format (see below)

You can freely combine the different arguments:
```bash
./Fbrary --libraryFile $LIBRARY_FILENAME list --unrated --completed
./Fbrary --libraryFile $LIBRARY_FILENAME list "Story"
./Fbrary --libraryFile $LIBRARY_FILENAME list --notcompleted
./Fbrary --libraryFile $LIBRARY_FILENAME list "Story" --completed
```

### Format
Use a format string to define how to output each book. Any string can be supplied and the following placeholders will be replaced:
```
%artist%, %album%, %title, %albumartist%, %id%, %duration%, %rating%, %comment%, %genre%
```
If an audio book does not have a proper value for the given placeholder (e.g. `%title%`) it is replaced by `<no title set>`. You can wrap a single or multiple placeholders in `??` to supress the `<no $PLACEHOLDER set>` text. E.g.:
```
??%genre%?? -> "" if the genre is not set
```
You can add any text you want inside the double question marks:
```
??(Genre: %genre%)?? -> (Genre: SciFi) if the genre is set
                     -> "" if the genre is not set
```
If you have multiple placeholders inside the same `??` the string will be empty if one or more placeholders are replaced with empty values:
```
??%genre% %artist% %album%?? -> "" if either genre, artist or album is empty
```

### Table
The format string uses the same placeholders like `--format` but `??` is not supported. Fields that are empty are blank by default. Any characters besides placeholders are ignored. E.g.
```
--table "%artist% %album%"
```
is the same as the following two:
```
--table "%artist%%album%"
--table "%artist% (Artist) | %album% (Album)"
```
Columns are limited to 64 characters (for the content additional characters are used as padding and border) by default. Use the `--maxcolwidth` (or `-w`) switch to override the value. Any value less than four will be changed to four.

Update
------
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
./Fbrary --libraryFile $LIBRARY_FILENAME completed $ID1 $ID2 $..
```
Mark one or more audio books as completed.
Use the `list` command to find `$ID`s.

NotCompleted
------------
```bash
./Fbrary --libraryFile $LIBRARY_FILENAME notcompleted $ID1 $ID2 $..
```
Mark one or more audio books as not completed.
Use the `list` command to find `$ID`s.

Aborted
-------
```bash
./Fbrary --libraryFile $LIBRARY_FILENAME aborted $ID1 $ID2 $..
```
Mark one or more audio books as aborted. Use this to mark books that you no longer want to listen to.
Use the `list` command to find `$ID`s.

Unmatched
-------
```bash
./Fbrary --libraryFile $LIBRARY_FILENAME unmatched $PATH
```
Checks whether each mp3/ogg file in the given directory (and its subdirectories) is part of an audio book in the library. Use to find files that you have newly added to your files but not yet your library.

Files
-----
```bash
./Fbrary --libraryFile $LIBRARY_FILENAME files $ID
./Fbrary --libraryFile $LIBRARY_FILENAME files $ID --
```

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

