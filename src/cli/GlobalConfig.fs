namespace b0wter.Fbrary

open System

module GlobalConfig =
    
    let private directory = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)
    let private file = "config.json"
    let globalConfigPath = IO.Path.combine (directory, file)
    
    type GlobalConfig = {
        LibraryFile: string
    }
    
    let deserialize (json: string) : GlobalConfig option =
        match Microsoft.FSharpLu.Json.Compact.tryDeserialize<GlobalConfig> json with
        | Choice1Of2 c -> Some c
        | Choice2Of2 _ -> None

    let tryLoad () =
        let tryRead = IO.readTextFromFile >> function | Ok c -> Some c | Error _ -> None
        
        if IO.File.exists globalConfigPath then
            globalConfigPath
            |> tryRead
            |> Option.bind deserialize
        else None
            