namespace b0wter.AudiobookLibrary

open b0wter.AudiobookLibrary.Arguments

module Config =
    
    type AddConfig = {
        Path: string
    }
    
    type ListConfig = {
        Filter: string
        Format: string
        Unrated: bool
        NotCompleted: bool
        Completed: bool
    }
    let private emptyListConfig =
        {
            Filter = System.String.Empty
            Format = defaultFormatString
            Unrated = false
            NotCompleted = false
            Completed = false
        }
    
    type RescanConfig = {
        Path: string
    }
    
    type UpdateConfig = {
        Id: int
    }
    
    type RateConfig = {
        Id: int option
    }
    
    type CompletedConfig = {
        Id: int
    }
    
    type NotCompletedConfig = {
        Id: int
    }
    
    type Command
        = Add of AddConfig
        | List of ListConfig
        | Rescan of RescanConfig
        | Update of UpdateConfig
        | Rate of RateConfig
        | Completed of CompletedConfig
        | NotCompleted of NotCompletedConfig
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
        | ListArgs.NotCompleted -> { config with NotCompleted = true }
        | ListArgs.Completed -> { config with Completed = true }
        | Unrated -> { config with Unrated = true }
    
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
        | MainArgs.Rate id ->
            { config with Command = Rate { Id = id } }
        | MainArgs.Completed id ->
            { config with Command = Completed { Id = id } }
        | MainArgs.NotCompleted id ->
            { config with Command = NotCompleted { Id = id } }
        | MainArgs.List l ->
            let listConfig = match config.Command with
                             | List c -> c
                             | Add _
                             | Rescan _
                             | Update _
                             | Uninitialized
                             | Rate _
                             | NotCompleted _
                             | Completed _ -> emptyListConfig
            let updatedListConfig = l.GetAllResults() |> List.fold applyListArg listConfig
            { config with Command = List updatedListConfig }
                   