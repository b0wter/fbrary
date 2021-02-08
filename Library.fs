namespace b0wter.AudiobookLibrary

open System

module Library =
    
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
    
    let containsId id (l: Library) : bool =
        l.Audiobooks |> List.map Audiobook.id |> List.contains id
    
    let addBook (a: Audiobook.Audiobook) (l: Library) : Result<Library, string> =
        match l.Audiobooks |> List.tryFind (Audiobook.isSameSource a) with
        | Some previousBook ->
            // In case there is a book with the same source we want to reuse the previous id.
            let a = { a with Id = previousBook.Id }
            let otherBooks = (l.Audiobooks |> List.filter (not << Audiobook.isSameSource a))
            if otherBooks |> List.map Audiobook.id |> List.contains a.Id then Error "The audiobook could not be added to the library because its id is already taken."
            else
                {
                    l with
                        Audiobooks = a :: otherBooks
                        LastScanned = DateTime.Now
                } |> Ok
        | None ->
            if l.Audiobooks |> List.map Audiobook.id |> List.contains a.Id then Error "The audiobook could not be added to the library because its id is already taken."
            else
                {
                    l with
                        Audiobooks = a :: l.Audiobooks
                        LastScanned = DateTime.Now
                } |> Ok