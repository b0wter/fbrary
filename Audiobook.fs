namespace b0wter.Audiobook

open b0wter.Audiobook

module Audiobook =
    open System
    
    [<Serializable>]
    type Audiobook = {
        Filename: string
        Artists: string option
        Album: string option
        Title: string option
        Duration: TimeSpan
        HasPicture: bool
    }
    
    let maxPropertyLength (a: Audiobook) =
        [ a.Album; a.Artists; a.Title ]
        |> List.choose id
        |> List.map (string >> String.length)
        |> (fun list -> if list.IsEmpty then 0 else list |> List.max)

    let serialize (a: Audiobook) : string =
        Microsoft.FSharpLu.Json.Default.serialize a
        
    let deserialize (jsonString: string) : Result<Audiobook, string> =
        let result = Microsoft.FSharpLu.Json.Default.tryDeserialize<Audiobook> jsonString
        match result with
        | Choice1Of2 a -> Ok a
        | Choice2Of2 e -> Error e