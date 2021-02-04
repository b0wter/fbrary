namespace b0wter.AudiobookLibrary

open Argu
 
module Arguments =
    
    let artistFormatString = "%artist%"
    let albumFormatString = "%album%"
    let titleFormatString = "%title%"
    let durationFormatString = "%duration%"
    let formatStringList = [artistFormatString; albumFormatString; titleFormatString; durationFormatString]
    let formattedFormatStringList = sprintf "%s, %s, %s, %s" artistFormatString albumFormatString titleFormatString durationFormatString
    let defaultFormatString = sprintf "%s - %s - %s - %s" titleFormatString artistFormatString albumFormatString durationFormatString
    
    let addArgumentHelp = "Add the file or directly to the library. " +
                          "Note that all files inside a folder are interpreted as a single audiobook. " +
                          "If you want to add sub folders as independent audiobooks add them one by one. " +
                          "Note that new entries overwrite previous entries. Filenames are used to check if the audiobook was previously added."
    
    type AddArgs =
        | Path of string
        | File of string
        interface IArgParserTemplate with
            member s.Usage =
                match s with
                | Path _ -> "Directory to add to the library. All files in the directory are treated as a single audiobook."
                | File _ -> "Add a single file a an audiobook to the library."
                
    type ListArgs =
        | Filter of string
        interface IArgParserTemplate with
            member s.Usage =
                match s with
                | Filter _ -> "Lists all audiobooks that match the given filter. An empty filter returns all audiobooks."
        
    type MainArgs =
        | [<AltCommandLine("-v")>] Verbose
        | [<AltCommandLine("-l")>] Library of string
        | [<AltCommandLine("-f")>] Format of string option
        | [<Last; CliPrefix(CliPrefix.None)>] Add of string
        | [<Last; CliPrefix(CliPrefix.None)>] List of string option
        interface IArgParserTemplate with
            member s.Usage =
                match s with
                | Add _ -> addArgumentHelp 
                | List _ -> "List all audiobooks in the current library."
                | Verbose -> "Verbose output."
                | Library _ -> "Library file to read/write to."
                | Format _ -> sprintf "Format the output by supplying a format string. The following placeholders are available: '%s'. Do not forget to quote the format string. Only used with the 'list' command." formattedFormatStringList
                

