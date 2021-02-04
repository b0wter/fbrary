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
        | Choice2Of2 e ->
            let rows = e.Split(Environment.NewLine)
            let index = rows.[0].LastIndexOf(':')
            if index > 0 then
                Error <| rows.[0].Remove(0, index + 1).TrimStart()
            else
                Error e

    let empty = { Audiobooks = []; LastScanned = DateTime.MinValue }
    
    let addBook (a: Audiobook.Audiobook) (l: Library) : Library =
        {
            l with
                Audiobooks = a :: (l.Audiobooks |> List.filter (not << Audiobook.isSameSource a))
                LastScanned = DateTime.Now
        }