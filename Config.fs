namespace b0wter.AudiobookLibrary

open b0wter.AudiobookLibrary.Arguments

module Config =
    
    type AddConfig = {
        Path: string
    }
    
    type ListConfig = {
        Filter: string
        Format: string
    }
    let private emptyListConfig = { Filter = System.String.Empty; Format = defaultFormatString }
    
    type RescanConfig = {
        Path: string
    }
    
    type UpdateConfig = {
        Id: int
    }
    
    type Command
        = Add of AddConfig
        | List of ListConfig
        | Rescan of RescanConfig
        | Update of UpdateConfig
        | Uninitialized
    
    type Config = {
        Command: Command
        Verbose: bool
        LibraryFile: string
        NonInteractive: bool
    }
    let empty = { Command = Uninitialized; Verbose = false; LibraryFile = System.String.Empty; NonInteractive = false }
    
    let applyListArg (config: ListConfig) (l: ListArgs) : ListConfig =
        match l with
        | Format format -> { config with Format = format }
        | Filter filter -> { config with Filter = filter }
    
    // Define functions that take arguments and apply them to a config.
    // Use this to fold the configuration from the arguments.
    let applyMainArg (config: Config) (m: MainArgs) : Config =
        match m with
        | Verbose ->
            { config with Verbose = true }
        | Library l ->
            { config with LibraryFile = l }
        | NonInteractive ->
            { config with NonInteractive = true }
        | MainArgs.Rescan path ->
            { config with Command = Rescan { Path = path } }
        | MainArgs.Update id ->
            { config with Command = Update { Id = id } }
        | MainArgs.Add path ->
            { config with Command = Add { Path = path } }
        | MainArgs.List l ->
            let listConfig = match config.Command with
                             | List c -> c
                             | Add _ | Rescan _ | Update _ | Uninitialized -> emptyListConfig
            let updatedListConfig = l.GetAllResults() |> List.fold applyListArg listConfig
            { config with Command = List updatedListConfig }
                   