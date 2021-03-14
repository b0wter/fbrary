namespace b0wter.Fbrary

open Argu
open b0wter.Fbrary.Arguments
open b0wter.FSharp

module Config =
    
    type AddConfig = {
        Path: string
        NonInteractive: bool
        SubDirectoriesAsBooks: bool
        FilesAsBooks: bool
    }
    let private emptyAddConfig = {
        Path = System.String.Empty
        NonInteractive = false
        SubDirectoriesAsBooks = false
        FilesAsBooks = false
    }
   
    type TableListFormat = {
        Format: string
        MaxColWidth: int
    }
     
    type ListFormat =
        | Cli of format:string
        | Table of TableListFormat
    
    let private defaultCliFormat = Formatter.CommandLine.defaultFormatString |> ListFormat.Cli
    
    let private defaultTableFormat =
        {
            Format = Formatter.CommandLine.defaultFormatString
            MaxColWidth = 42
        } 
    
    type ListConfig = {
        Filter: string 
        Ids: int list
        Formats: ListFormat list
        Unrated: bool
        NotCompleted: bool
        Completed: bool
    }
    let private emptyListConfig = {
        Filter = System.String.Empty
        Formats = []
        Ids = []
        Unrated = false
        NotCompleted = false
        Completed = false
    }
    
    type RemoveConfig = {
        Id: int
    }
    
    type UpdateConfig = {
        Ids: int list
        Fields: (string * string) list
    }
    let private emptyUpdateConfig = {
        Ids = []
        Fields = []
    }
    
    type RateConfig = {
        Id: int option
    }
    
    type CompletedConfig = {
        Ids: int list
    }
    
    type NotCompletedConfig = {
        Ids: int list
    }
    
    type AbortedConfig = {
        Ids: int list
    }
    
    type FilesConfig = {
        Id: int
        Separator: FileListingSeparator
    }
    
    let private emptyFilesConfig = {
        Id = -1
        Separator = NewLine
    }
    
    type UnmatchedConfig = {
        Path: string
    }
    
    type WriteConfig = {
        Fields: string list
        DryRun: bool
        NonInteractive: bool
    }
    let private emptyWriteConfig = {
        Fields = []
        DryRun = false
        NonInteractive = false
    }
    
    type Command
        = Add of AddConfig
        | List of ListConfig
        | Remove of RemoveConfig
        | Update of UpdateConfig
        | Rate of RateConfig
        | Completed of CompletedConfig
        | NotCompleted of NotCompletedConfig
        | Aborted of AbortedConfig
        | Unmatched of UnmatchedConfig
        | Files of FilesConfig
        | Uninitialized
        | Write of WriteConfig
        | Version
    
    type Config = {
        Command: Command
        Verbose: bool
        LibraryFile: string
    }
    let private empty = { Command = Uninitialized; Verbose = false; LibraryFile = System.String.Empty }
    
    let applyListArg (config: ListConfig) (l: ListArgs) : ListConfig =
        match l with
        | ListArgs.Cli format ->
            let rec step accumulator remaining =
                match remaining with
                | [] ->
                    (ListFormat.Cli format) :: accumulator
                | (ListFormat.Cli _) :: tail ->
                    accumulator @ (ListFormat.Cli format) :: tail
                | otherFormat :: tail ->
                    step (otherFormat :: accumulator) tail
            let updatedFormats = step [] config.Formats
            { config with Formats = updatedFormats }
        | ListArgs.Table format ->
            let rec step accumulator remaining =
                match remaining with
                | [] ->
                    (ListFormat.Table { defaultTableFormat with Format = format }) :: accumulator
                | (ListFormat.Table tableFormat) :: tail ->
                    accumulator @ ((ListFormat.Table { tableFormat with Format = format }) :: tail)
                | otherFormat :: tail ->
                    step (otherFormat :: accumulator) tail
            let updatedFormats = step [] config.Formats
            { config with Formats = updatedFormats }
        | ListArgs.MaxTableColumnWidth width ->
            let formats = config.Formats
                          |> List.map (function | ListFormat.Table f -> ListFormat.Table { f with MaxColWidth = width }
                                                | ListFormat.Cli   f -> ListFormat.Cli f)
            { config with Formats = formats }
        | Filter filter -> { config with Filter = filter }
        | ListArgs.Ids ids -> { config with Ids = ids }
        | ListArgs.NotCompleted -> { config with NotCompleted = true }
        | ListArgs.Completed -> { config with Completed = true }
        | Unrated -> { config with Unrated = true }
        
    let applyAddArg (config: AddConfig) (a: AddArgs) : AddConfig =
        match a with
        | Path p -> { config with Path = p }
        | AddArgs.NonInteractive -> { config with NonInteractive = true }
        | SubDirectoriesAsBooks -> { config with SubDirectoriesAsBooks = true }
        | FilesAsBooks -> { config with FilesAsBooks = true }
        
    let applyFilesArg (config: FilesConfig) (f: FilesArgs) : FilesConfig =
        match f with
        | Id id -> { config with Id = id }
        | Separator s -> { config with Separator = s }
        
    let applyWriteArg (config: WriteConfig) (w: WriteArgs) : WriteConfig =
        match w with
        | Fields fields -> { config with Fields = fields }
        | DryRun -> { config with DryRun = true }
        | NonInteractive -> { config with NonInteractive = true }
        
    let applyUpdateArg (config: UpdateConfig) (u: UpdateArgs) : UpdateConfig =
        match u with
        | UpdateArgs.Ids id -> { config with Ids = id }
        | UpdateArgs.Field (field, value) -> { config with Fields = (field, value) :: config.Fields }
    
    // Define functions that take arguments and apply them to a config.
    // Use this to fold the configuration from the arguments.
    let applyMainArg (config: Config) (m: MainArgs) : Config =
        match m with
        | Verbose ->
            { config with Verbose = true }
        | Library l ->
            { config with LibraryFile = l }
        | MainArgs.Remove id ->
            { config with Command = Remove { Id = id } }
        | MainArgs.Update update ->
            let updateConfig = match config.Command with
                               | Update u -> u
                               | _ -> emptyUpdateConfig
            let updatedUpdateConfig = update.GetAllResults() |> List.fold applyUpdateArg updateConfig
            { config with Command = Update updatedUpdateConfig }
        | MainArgs.Add add ->
            let addConfig = match config.Command with
                            | Add a -> a
                            | _ -> emptyAddConfig
            let updatedAddConfig = add.GetAllResults() |> List.fold applyAddArg addConfig
            { config with Command = Add updatedAddConfig }
        | MainArgs.Rate id ->
            { config with Command = Rate { Id = id } }
        | MainArgs.Completed ids ->
            { config with Command = Completed { Ids = ids } }
        | MainArgs.NotCompleted ids ->
            { config with Command = NotCompleted { Ids = ids } }
        | MainArgs.Aborted ids ->
            { config with Command = Aborted { Ids = ids } }
        | MainArgs.Files files ->
            let filesConfig = match config.Command with
                              | Files f -> f
                              | _ -> emptyFilesConfig
            let updatedFilesConfig = files.GetAllResults() |> List.fold applyFilesArg filesConfig
            { config with Command = Files updatedFilesConfig  }
        | MainArgs.Unmatched path ->
            { config with Command = Unmatched { Path = path } }
        | MainArgs.List l ->
            let listConfig = match config.Command with
                             | List c -> c
                             | _ -> emptyListConfig
            let updatedListConfig = l.GetAllResults() |> List.fold applyListArg listConfig
            let updatedListConfig = { updatedListConfig with Formats = updatedListConfig.Formats |> List.rev }
            do printfn "%A" updatedListConfig
            { config with Command = List updatedListConfig }
        | MainArgs.Write w ->
            let writeConfig = match config.Command with
                              | Write w -> w
                              | _ -> emptyWriteConfig
            let updatedWriteConfig = w.GetAllResults() |> List.fold applyWriteArg writeConfig
            { config with Command = Write updatedWriteConfig }
        | MainArgs.Version ->
            { config with Command = Version }
            
    let applyAllArgs (results: ParseResults<MainArgs>) =
        //
        // This is rather hacky but it makes sure that Argu checks the library parameter
        // if the command is not the version command.
        //
        let args = results.GetAllResults ()
        if args |> List.exists (function MainArgs.Version -> true | _ -> false) then
            { empty with Command = Version }
        else
            let _ = results.GetResult(<@ MainArgs.Library @>)
            args |> List.fold applyMainArg empty
            
