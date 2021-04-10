namespace b0wter.Fbrary

open Argu
 
module Arguments =
       
    let addArgumentHelp = "Add the file or directly to the library. " +
                          "Note that all files inside a folder are interpreted as a single audiobook. " +
                          "If you want to add sub folders as independent audiobooks add them one by one. " +
                          "Note that new entries overwrite previous entries. Filenames are used to check if the audiobook was previously added."
    let formattedFormatStringList = System.String.Join(", ", Formatter.allFormantPlaceholders)
    let formattedFieldList = System.String.Join(", ", Formatter.allFieldPlaceholders)
       
    type AddArgs =
        | [<MainCommand>] Path of string
        | [<AltCommandLine("-n"); Unique>] NonInteractive
        | [<CustomCommandLine("--subdirectories-as-books"); Unique>] SubDirectoriesAsBooks
        | [<CustomCommandLine("--files-as-books"); Unique>] FilesAsBooks
        interface IArgParserTemplate with
            member s.Usage =
                match s with
                | Path _ -> "The path of the file/directory to add."
                | NonInteractive -> "Adds audiobooks without asking the user to check the metadata."
                | SubDirectoriesAsBooks -> "Each subfolder inside the given path is interpreted as an independent audio book."
                | FilesAsBooks -> "Each media file in the given path is interpreted as an independent audio book."
        
    type ListArgs =
        | [<MainCommand>] Filter of string
        | [<AltCommandLine("-c"); Unique>] Cli of format:string
        | [<AltCommandLine("-t"); Unique>] Table of format:string 
        | [<AltCommandLine("-h"); Unique>] Html of input:string * output:string
        | [<AltCommandLine("-s"); Unique>] Sort of string list
        | [<CustomCommandLine("--max-col-width"); AltCommandLine("-w"); Unique>] MaxTableColumnWidth of int
        | [<Unique>] Ids of int list
        | [<Unique>] Rated
        | [<Unique>] Unrated
        | [<Unique>] NotCompleted
        | [<Unique>] Completed
        | [<Unique>] Aborted
        interface IArgParserTemplate with
            member s.Usage =
                match s with
                | Filter _ -> "Lists all audiobooks that match the given filter. An empty filter returns all audiobooks."
                | Cli _ -> sprintf "Format the output by supplying a format string. The following placeholders are available: '%s'. Do not forget to quote the format string." formattedFormatStringList
                | Table _ -> sprintf "Format the output as a table. Use the following placeholders: '%s'. Do not forget to quote the format string. You can only use either 'format' or this option." formattedFormatStringList
                | Html _ -> "Use a razor template. Required two arguments: input file and output file. See readme for details."
                | Sort _ -> sprintf "Define the order in which the books are sorted. You can supply multiple parameters and the books will be sorted by all of them in order. The default sort order is ascending. To sort descending add `:d` to the field name (e.g. \"album:d\"). You can use any of the placeholder fields: '%s'" formattedFieldList
                | MaxTableColumnWidth _ -> sprintf "Maximum size for table columns. Only used together with the --table option. Minimum value: 4."
                | Ids _ -> "Only list audio books with the given ids."
                | Rated -> "Only list books that have been rated."
                | Unrated -> "Only list books that have not yet been rated."
                | NotCompleted -> "Only list books that have not yet been completely listened to."
                | Completed -> "Only list books that have been completely listened to."
                | Aborted -> "Only list books that have been marked as aborted."
                
    type UpdateArgs =
        | [<MainCommand>] Ids of int list
        | Field of field:string * value:string
        interface IArgParserTemplate with
            member s.Usage =
                match s with
                | Ids _ -> "Ids of the entries to edit. Use the `list` command to find ids."
                | Field _ -> "Update a field immediately instead of using the interactive process. Takes two parameters: the name of the field and the value. Add quotes."
    
    type FileListingSeparator =
        | Space
        | NewLine
                
    type FilesArgs =
        | [<MainCommand>] Ids of int list
        | [<AltCommandLine("-s")>] Separator of FileListingSeparator
        | Missing
        interface IArgParserTemplate with
            member s.Usage =
                match s with
                | Ids _ -> "Ids of the audiobooks whose files you want to list. Use the `list` command to find ids."
                | Separator _ -> "Define the separator for listing multiple files. Defaults to 'newline'. Possible values are: 'space' and 'newline'."
                | Missing -> "Does not print the name of the files but lists all missing files (meaning files that are listed in the library but that do not exist on disk)."

    type WriteArgs =
        | [<MainCommand>] Fields of string list
        | [<AltCommandLine("-d"); Unique>] DryRun
        | [<AltCommandLine("-n"); Unique>] NonInteractive
        interface IArgParserTemplate with
            member s.Usage =
                match s with
                | Fields _ -> "Name of the fields you want to write to the files. Uses the same format as the `list` command. If you do not supply an argument all fields are written."
                | DryRun -> "Do not modify files just print the changes."
                | NonInteractive -> "Skip all user interaction."
        
    type MainArgs =
        | [<AltCommandLine("-V")>] Verbose
        | [<AltCommandLine("-l"); First; AltCommandLine("--library-file")>] Library of string
        | [<CliPrefix(CliPrefix.None)>] Add of ParseResults<AddArgs>
        | [<CliPrefix(CliPrefix.None)>] List of ParseResults<ListArgs>
        | [<CliPrefix(CliPrefix.None)>] Remove of int
        | [<CliPrefix(CliPrefix.None)>] Update of ParseResults<UpdateArgs>
        | [<CliPrefix(CliPrefix.None)>] Rate of int option
        | [<CliPrefix(CliPrefix.None)>] Completed of int list
        | [<CliPrefix(CliPrefix.None)>] NotCompleted of int list
        | [<CliPrefix(CliPrefix.None)>] Aborted of int list
        | [<CliPrefix(CliPrefix.None)>] Files of ParseResults<FilesArgs>
        | [<CliPrefix(CliPrefix.None)>] Unmatched of string
        | [<CliPrefix(CliPrefix.None)>] Write of ParseResults<WriteArgs>
        | [<CliPrefix(CliPrefix.None)>] Migrate
        | [<CliPrefix(CliPrefix.None)>] Details of int list
        | [<CliPrefix(CliPrefix.DoubleDash); First>] Version
        interface IArgParserTemplate with
            member s.Usage =
                match s with
                | Verbose -> "Verbose output."
                | Library _ -> "Library file to read/write to."
                | Add _ -> addArgumentHelp 
                | List _ -> "List all audiobooks in the current library."
                | Remove _ -> "Removes an audio book from the library."
                | Update _ -> "Use an interactive prompt to update the metadata of a library item. Requires an item id."
                | Rate _ -> "Rate one or more books. If you supply you rate a single book otherwise all unrated books are listed."
                | Completed _ -> "Mark the book with the given id as completely listened to."
                | NotCompleted _ -> "Mark the book with the given id as not completely listened to."
                | Aborted _ -> "Mark the book with the given id as aborted meaning you stopped listening to it."
                | Files _ -> "List all files of an audio book. Use the `list` command to find book ids."
                | Unmatched _ -> "Reads all mp3/ogg files in the given paths and checks if all files are known to the library."
                | Write _ -> "Write the meta data stored in the library to the actual mp3/ogg files."
                | Migrate -> "Migrate an old library file to the current format."
                | Details _ -> "List the complete details (including files) for the given audio books."
                | Version -> "Echo the version of this software."
                