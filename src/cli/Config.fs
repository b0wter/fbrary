namespace b0wter.Fbrary

open b0wter.Fbrary.Arguments

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
    
    type ListConfig = {
        Filter: string 
        Format: string option
        Table: string option
        MaxTableColumnWidth: int
        Ids: int list
        Unrated: bool
        NotCompleted: bool
        Completed: bool
    }
    let private emptyListConfig = {
        Filter = System.String.Empty
        Format = None
        Table = None
        MaxTableColumnWidth = 64
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
        Field: (string * string) option
    }
    let private emptyUpdateConfig = {
        Ids = []
        Field = None
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
    
    type Config = {
        Command: Command
        Verbose: bool
        LibraryFile: string
    }
    let empty = { Command = Uninitialized; Verbose = false; LibraryFile = System.String.Empty }
    
    let applyListArg (config: ListConfig) (l: ListArgs) : ListConfig =
        match l with
        | Format format -> { config with Format = Some format }
        | Filter filter -> { config with Filter = filter }
        | Table table -> { config with Table = Some table }
        | MaxTableColumnWidth size -> { config with MaxTableColumnWidth = if size >= 4 then size else 4 }
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
        | UpdateArgs.Field (field, value) -> { config with Field = Some (field, value) }
    
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
            { config with Command = List updatedListConfig }
        | MainArgs.Write w ->
            let writeConfig = match config.Command with
                              | Write w -> w
                              | _ -> emptyWriteConfig
            let updatedWriteConfig = w.GetAllResults() |> List.fold applyWriteArg writeConfig
            { config with Command = Write updatedWriteConfig }
            
                   