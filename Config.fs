namespace b0wter.Fbrary

open b0wter.Fbrary.Arguments

module Config =
    
    type AddConfig = {
        Path: string
        NonInteractive: bool
    }
    let private emptyAddConfig =
        {
            Path = System.String.Empty
            NonInteractive = false
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
    
    type RemoveConfig = {
        Id: int
    }
    
    type UpdateConfig = {
        Id: int
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
    
    type UnmatchedConfig = {
        Path: string
    }
    
    type Command
        = Add of AddConfig
        | List of ListConfig
        | Remove of RemoveConfig
        | Update of UpdateConfig
        | Rate of RateConfig
        | Completed of CompletedConfig
        | NotCompleted of NotCompletedConfig
        | Unmatched of UnmatchedConfig
        | Uninitialized
    
    type Config = {
        Command: Command
        Verbose: bool
        LibraryFile: string
    }
    let empty = { Command = Uninitialized; Verbose = false; LibraryFile = System.String.Empty }
    
    let applyListArg (config: ListConfig) (l: ListArgs) : ListConfig =
        match l with
        | Format format -> { config with Format = format }
        | Filter filter -> { config with Filter = filter }
        | ListArgs.NotCompleted -> { config with NotCompleted = true }
        | ListArgs.Completed -> { config with Completed = true }
        | Unrated -> { config with Unrated = true }
        
    let applyAddArg (config: AddConfig) (a: AddArgs) : AddConfig =
        match a with
        | Path p -> { config with Path = p }
        | NonInteractive -> { config with NonInteractive = true }
    
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
        | MainArgs.Update id ->
            { config with Command = Update { Id = id } }
        | MainArgs.Add add ->
            let addConfig = match config.Command with
                            | Add a -> a
                            | List _
                            | Remove _
                            | Update _
                            | Uninitialized
                            | Rate _
                            | Unmatched _
                            | NotCompleted _
                            | Completed _ -> emptyAddConfig
            let updatedAddConfig = add.GetAllResults() |> List.fold applyAddArg addConfig
            { config with Command = Add updatedAddConfig }
        | MainArgs.Rate id ->
            { config with Command = Rate { Id = id } }
        | MainArgs.Completed ids ->
            { config with Command = Completed { Ids = ids } }
        | MainArgs.NotCompleted ids ->
            { config with Command = NotCompleted { Ids = ids } }
        | MainArgs.Unmatched path ->
            { config with Command = Unmatched { Path = path } }
        | MainArgs.List l ->
            let listConfig = match config.Command with
                             | List c -> c
                             | Add _
                             | Remove _
                             | Update _
                             | Uninitialized
                             | Rate _
                             | Unmatched _
                             | NotCompleted _
                             | Completed _ -> emptyListConfig
            let updatedListConfig = l.GetAllResults() |> List.fold applyListArg listConfig
            { config with Command = List updatedListConfig }
                   