namespace b0wter.Fbrary

open Argu
 
module Arguments =
       
    let addArgumentHelp = "Add the file or directly to the library. " +
                          "Note that all files inside a folder are interpreted as a single audiobook. " +
                          "If you want to add sub folders as independent audiobooks add them one by one. " +
                          "Note that new entries overwrite previous entries. Filenames are used to check if the audiobook was previously added."
    let formattedFormatStringList = System.String.Join(", ", Formatter.allFormantPlaceholders)
       
    type AddArgs =
        | [<MainCommand>] Path of string
        | [<AltCommandLine("-n")>] NonInteractive
        | [<CustomCommandLine("--subdirectories-as-books")>] SubDirectoriesAsBooks
        | [<CustomCommandLine("--files-as-books")>] FilesAsBooks
        interface IArgParserTemplate with
            member s.Usage =
                match s with
                | Path _ -> "The path of the file/directory to add."
                | NonInteractive -> "Adds audiobooks without asking the user to check the metadata."
                | SubDirectoriesAsBooks -> "Each subfolder inside the given path is interpreted as an independent audio book."
                | FilesAsBooks -> "Each media file in the given path is interpreted as an independent audio book."
        
    type ListArgs =
        | [<MainCommand>] Filter of string
        | [<AltCommandLine("-f")>] Format of string
        | [<AltCommandLine("-t")>] Table of string
        | [<CustomCommandLine("--max-col-width"); AltCommandLine("-w")>] MaxTableColumnWidth of int
        | Ids of int list
        | Unrated
        | NotCompleted
        | Completed
        interface IArgParserTemplate with
            member s.Usage =
                match s with
                | Filter _ -> "Lists all audiobooks that match the given filter. An empty filter returns all audiobooks."
                | Format _ -> sprintf "Format the output by supplying a format string. The following placeholders are available: '%s'. Do not forget to quote the format string. You can only use either 'table' or this option." formattedFormatStringList
                | Table _ -> sprintf "Format the output as a table. Use the following placeholders: '%s'. Do not forget to quote the format string. You can only use either 'format' or this option." formattedFormatStringList
                | MaxTableColumnWidth _ -> sprintf "Maximum size for table columns. Only used together with the --table option. Minimum value: 4."
                | Ids _ -> "Only list audio books with the given ids."
                | Unrated -> "Only list books that have not yet been rated."
                | NotCompleted -> "Only list books that have not yet been completely listened to."
                | Completed -> "Only list books that have been completely listened to."
                
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
        | [<MainCommand>] Id of int
        | [<AltCommandLine("-s")>] Separator of FileListingSeparator
        interface IArgParserTemplate with
            member s.Usage =
                match s with
                | Id _ -> "Id of the audiobook whose files you want to list. Use the `list` command to find ids."
                | Separator _ -> "Define the separator for listing multiple files. Defaults to 'newline'. Possible values are: 'space' and 'newline'."

    type WriteArgs =
        | [<MainCommand>] Fields of string list
        | [<AltCommandLine("-d")>] DryRun
        | [<AltCommandLine("-n")>] NonInteractive
        interface IArgParserTemplate with
            member s.Usage =
                match s with
                | Fields _ -> "Name of the fields you want to write to the files. Uses the same format as the `list` command. If you do not supply an argument all fields are written."
                | DryRun -> "Do not modify files just print the changes."
                | NonInteractive -> "Skip all user interaction."
        
    type MainArgs =
        | [<AltCommandLine("-V")>] Verbose
        | [<AltCommandLine("-l"); Mandatory; First; AltCommandLine("--library-file")>] Library of string
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
                

