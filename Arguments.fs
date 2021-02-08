namespace b0wter.AudiobookLibrary

open Argu
 
module Arguments =
    
    let artistFormatString = "%artist%"
    let albumFormatString = "%album%"
    let titleFormatString = "%title%"
    let durationFormatString = "%duration%"
    let idFormatString = "%id%"
    let ratingFormatString = "%rating%"
    let commentFormatString = "%comment%"
    
    let formattedFormatStringList = sprintf "%s, %s, %s, %s, %s, %s, %s" artistFormatString albumFormatString titleFormatString durationFormatString idFormatString ratingFormatString commentFormatString
    let defaultFormatString = sprintf "(%s) %s - %s - %s - %s" idFormatString titleFormatString artistFormatString albumFormatString durationFormatString
    
    let addArgumentHelp = "Add the file or directly to the library. " +
                          "Note that all files inside a folder are interpreted as a single audiobook. " +
                          "If you want to add sub folders as independent audiobooks add them one by one. " +
                          "Note that new entries overwrite previous entries. Filenames are used to check if the audiobook was previously added."
    
    type ListArgs =
        | [<MainCommand; First>] Filter of string
        | Format of string
        | Unrated
        | NotCompleted
        | Completed
        interface IArgParserTemplate with
            member s.Usage =
                match s with
                | Filter _ -> "Lists all audiobooks that match the given filter. An empty filter returns all audiobooks."
                | Format _ -> sprintf "Format the output by supplying a format string. The following placeholders are available: '%s'. Do not forget to quote the format string. Only used with the 'list' command." formattedFormatStringList
                | Unrated -> "Only list books that have not yet been rated."
                | NotCompleted -> "Only list books that have not yet been completely listened to."
                | Completed -> "Only list books that have been completely listened to."
        
    type MainArgs =
        | [<AltCommandLine("-V")>] Verbose
        | [<AltCommandLine("-l"); Mandatory>] Library of string
        | [<AltCommandLine("-n")>] NonInteractive
        | [<Last; CliPrefix(CliPrefix.None)>] Add of string
        | [<Last; CliPrefix(CliPrefix.None)>] List of ParseResults<ListArgs>
        | [<Last; CliPrefix(CliPrefix.None)>] Rescan of string
        | [<Last; CliPrefix(CliPrefix.None)>] Update of int
        | [<Last; CliPrefix(CliPrefix.None)>] Rate of int option
        | [<Last; CliPrefix(CliPrefix.None)>] Completed of int
        | [<Last; CliPrefix(CliPrefix.None)>] NotCompleted of int
        interface IArgParserTemplate with
            member s.Usage =
                match s with
                | Verbose -> "Verbose output."
                | Library _ -> "Library file to read/write to."
                | NonInteractive -> "Adds audiobooks without asking the user to check the metadata."
                | Add _ -> addArgumentHelp 
                | List _ -> "List all audiobooks in the current library."
                | Rescan _ -> "Read the metadata from the files again and update the library contents."
                | Update _ -> "Use an interactive prompt to update the metadata of a library item. Required an item id."
                | Rate _ -> "Rate one or more books. If you supply you rate a single book otherwise all unrated books are listed."
                | Completed _ -> "Mark the book with the given id as completely listened to."
                | NotCompleted _ -> "Mark the book with the given id as not completely listened to."
                

