namespace b0wter.Audiobook

module Library =
    open System
    
    type Library = {
        Audiobooks: Audiobook.Audiobook list
        LastScanned: DateTime
    }
    
    let serialize (l: Library) : string =
        Microsoft.FSharpLu.Json.Compact.serialize l
        
    let deserialize jsonString : Result<Library, string> =
        match Microsoft.FSharpLu.Json.Compact.tryDeserialize<Library> jsonString with
        | Choice1Of2 l -> Ok l
        | Choice2Of2 e -> Error e
