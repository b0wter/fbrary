Audio Book Library
==================
This is a simple CLI tool that allows you to manage your audio book library.

![Example using the table formatter](https://raw.githubusercontent.com/b0wter/fbrary/master/example.png)

Commands
========
The application is run in a style similar to `git`. There are several commands to manipulate/list your library.
All commands require you to specify the library file you want to work on. 
Use the `--library-file` (or `-l`) parameter before specifying any command.
Currently, the following commands are supported.

*To create your initial library use the `add` command. It will create a new library file if the file does not already exist!*

Add
---
```bash
./fbrary -l $LIBRARY_FILENAME add $PATH
```
Add a file or directory to the library.
If you specify a directory it will be recursively scanned for mp3 files and added as a single audio book.
After reading all the files you will be asked to confirm the meta data. 
If you want to skip this step you need to pass the `--noninteractive` (`-n`) argument:
```bash
./fbrary -l $LIBRARY_FILENAME add -n $PATH
```
To add multiple books at once there are two options. First, you can use your shell to find the files and add them by invoking fbrary. This will give you the most freedom as you can leverage the power of existing tools. Scroll to the bottom and check the **hots** section.
Second, there are two arguments that take care of the three most common tasks:
 * `--subdirectories-as-books` will only read the first level of subdirectories and add each subdirectory as an independent book. Each subdirecry will be scanned recursively.
 * `--files-as-books` will only read files from the given path and ignore all subdirectories. Each file is added as an independent audio book.
 * You can combine both arguments and both operations will be executed. Each subdirectory is added as an independent book (even if there are multiple mp3/ogg inside a folder) and all files in the root folder are added as independent books.

Remove
------
```bash
./fbrary -l $LIBRARY_FILENAME remove $ID
```
Removes an audio book from the library. Use the `list` command to find book ids.

List
----
```bash
./fbrary -l $LIBRARY_FILENAME list $PATTERN
```
Lists all entries in the library that contain the `$PATTERN` in either their _Artist_, _Album_, _AlbumArtist_ or _Title_ field.
The `$PATTERN` is an optional parameter. If it is skipped all books are listed.
There are the following additional parameters:

 * `--completed` - lists books that have been marked completed
 * `--notcompleted` - lists books that have not been marked completed
 * `--unrated` - lists books that do not have a rating
 * `--ids $ID1 $ID2 $..` - list only books with the given idst

You can freely combine the different arguments:
```bash
./fbrary -l $LIBRARY_FILENAME list --unrated --completed
./fbrary -l $LIBRARY_FILENAME list "Story"
./fbrary -l $LIBRARY_FILENAME list --notcompleted
./fbrary -l $LIBRARY_FILENAME list "Story" --completed
```

The make the program produce any output at all you need to define at least one output:

 * `--cli $FORMAT_STRING` - configure how to display the results (see below)
 * `--table $FORMAT_STRING` - display results as table with the given format (see below)
 * `--html $TEMPLATE_FILE $OUTPUT_FILE` - create an html file using the given template file (see below)

### Sort order
You can define the sort order using the `--sort` parameter. You can supply as many arguments as you like. The books are sorted by the first argument, then by the second and so on. The default sort order is _ascending_. If you want to sort _desceding_ you need to add `:d` to the field name.
```bash
# Sort by id (ascending).
./fbrary -l $LIBRARY_FILENAME list --sort id
# Sort by album artist first, items with the same album artist are sorted by id (both ascending).
./fbrary -l $LIBRARY_FILENAME list --sort albumartist id
# Sory by rating (descending).
./fbrary -l $LIBRARY_FILENAME list --sort rating:d
```

### Format
Use a format string to define how to output each book. Any string can be supplied and the following placeholders will be replaced:
```
%artist%, %album%, %title, %albumartist%, %id%, %duration%, %rating_string%, %rating_dots%, %comment%, %genre%, %completed_string%, %completed_symbols%
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
Columns are limited to 64 characters (for the content additional characters are used as padding and border) by default. Use the `--max-col-width` (or `-w`) switch to override the value. Any value less than four will be changed to four.

### Html
The `input` file needs to be a valid razor page. Fbrary uses [RazorLight](https://github.com/toddams/RazorLight) to produce html pages. Follow this [link](https://www.learnrazorpages.com/razor-pages) to get an overview of how razor pages work. Inside the razor page you have access to the following viewmodel:
```fsharp
type BookViewmodel = {
    Artist: string
    Album: string
    AlbumArtist: string
    Completed: bool
    Aborted: bool
    Comment: string
    Rating: int
    Title: string
    Duration: TimeSpan
    Id: int
    Genre: string
}

type Viewmodel = {
    Books: BookViewmodel list
    Generated: DateTime
}
```
You can find examples [here](https://github.com/b0wter/fbrary/tree/master/src/cli/html_templates/).
The results are written to the `output` file. Example:

```bash
./fbrary -l library.json list --html html_templates/simple_table.cshtml ./library.html
```

Update
------
```bash
./fbrary -l $LIBRARY_FILENAME update $ID1 $ID2 $..
```
Use the update command to edit the meta data for an existing library entry. You can supply multiple ids. Defaults to interactive evaluation. Meaning that you will be prompted for each field. Alternatively you can use the `--field $KEY $VALUE` parameter to update the given field to the given value. Supported keys are:
 * artist
 * album
 * albumartist
 * title
 * genre
 * comment
 * rating

You can supply the `--field` argument multiple times.
Use the `list` command to find `$ID`s.

Rate
----
```bash
./fbrary -l $LIBRARY_FILENAME rate $ID
```
Set a rating for the given audio book.
Use the `list` command to find `$ID`s.

Completed
---------
```bash
./fbrary -l $LIBRARY_FILENAME completed $ID1 $ID2 $..
```
Mark one or more audio books as completed.
Use the `list` command to find `$ID`s.

NotCompleted
------------
```bash
./fbrary -l $LIBRARY_FILENAME notcompleted $ID1 $ID2 $..
```
Mark one or more audio books as not completed.
Use the `list` command to find `$ID`s.

Aborted
-------
```bash
./fbrary -l $LIBRARY_FILENAME aborted $ID1 $ID2 $..
```
Mark one or more audio books as aborted. Use this to mark books that you no longer want to listen to.
Use the `list` command to find `$ID`s.

Unmatched
---------
```bash
./fbrary -l $LIBRARY_FILENAME unmatched $PATH
```
Checks whether each mp3/ogg file in the given directory (and its subdirectories) is part of an audio book in the library. Use to find files that you have newly added to your files but not yet your library.
Path comparison is done using absolute paths. Each file in the library is joined with the directiory of the `$LIBRARY_FILENAME`.
Given the library file `library.json` in the folder `/home/user/audiobooks` and the following entry in the file:
```
{
 "Id": 1,
  "Source": {
    "SingleFile": "./foo.mp3"
  },
  ...
}
```
will result in the path `/home/user/audiobooks/foo.mp3`.
The path argument given to the tool will also be expanded to an absolute path.

Files
-----
```bash
./fbrary -l $LIBRARY_FILENAME files $ID
./fbrary -l $LIBRARY_FILENAME files $ID --
```
List all files of the given audio book.
Use the `list` command to find `$ID`s.

Write
-----
```bash
./fbrary -l $LIBRARY_FILENAME write
./fbrary -l $LIBRARY_FILENAME write artist album
```
Writes the meta data in your library back to the files of the audio book. You can specify which fields you want to be written to the file by appending their names. See the example above. Allowed values are:
```
artist
album
albumArtist
title
genre
comment
```
To preview the changes add the `-d` (dry run) parameter.

Hints
=====

version
-------
To make the program echo the current version use the `--version` parameter:
```bash
./fbrary --version
```

bash
----
*Note: this works perfectly fine using the linux subsystem for Windows (WSL). You will however need to use the linux binaries instead of the windows binaries.*

If you want to quickly add many folders without checking their meta information use this command (bash required):
```bash
find $FOLDER_TO_LOOK_INSIDE -maxdepth 1 -mindepth 1 -type d -exec ./fbrary -l $LIBRARY_FILENAME add -n '{}' \;
```
This will add each folder as a separate item in the library.the

If you store many mp3/ogg files in a folder and want to add each file as its own audio book use this command (bash required):
```bash
find $FOLDER_TO_LOOK_INSIDE -maxdepth 1 -name "*.mp3" -exec ./fbrary -l $LIBRARY_FILENAME add -n '{}' \;
```

powershell
----------
If you want to add multiple mp3 files in the same folder as independent books use this command and replace the variables accordingly:
```
Get-ChildItem $PATH_TO_FOLDER *.mp3 | Select-Object FullName -expandproperty FullName | % {./fbrary -l $LIBRARY_FILE add -n $_ }
```
To add each folder in a folder as an independent book use the following command:
```
Get-ChildItem $BASE_FOLDER -Directory | Select-Object FullName -expandproperty FullName | % {./fbrary -l $LIBRARY_FILE add -n $_ }
```

cmd
---
I am unsure whether the default windows temrinal prompt allows you to do the above. If you know how please let me know.
