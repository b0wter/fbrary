namespace b0wter.Audiobook

module Arguments =

    open Argu
    
    (*
    type PathTypeArgument =
        | AudiobookDirectory
        | AudiobookCdDirectory
    
    type AddArgs =
        | [<MainCommand; ExactlyOnce>] Path of string
        | [<ExactlyOnce>] Type of PathTypeArgument
        interface IArgParserTemplate with
            s.Usage =
                match s with
                | Path _ -> "Path to add to the library. Can be a folder of a file. Default behaviour for folders is to interpret all files inside a folder as a single audiobook."
                | Type _ -> "Type of resource to add. Audiobook directory is a file with no subfolders "
    *)
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
        | [<Last; CliPrefix(CliPrefix.None)>] Add of string
        | [<Last; CliPrefix(CliPrefix.None)>] List of string option
        interface IArgParserTemplate with
            member s.Usage =
                match s with
                | Add _ -> "Add the file or directly to the library. Note that all files inside a folder are interpreted as a single audiobook. If you want to add sub folders as independent audiobooks add them one by one."
                | List _ -> "List all audiobooks in the current library."
                | Verbose -> "Verbose output."
                

