namespace b0wter.Fbrary

open System
open FsToolkit.ErrorHandling

module Library =
    
    type Library = {
        Audiobooks: Audiobook.Audiobook list
        LastScanned: DateTime
        BasePath: string option
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

    let empty = { Audiobooks = []; LastScanned = DateTime.MinValue; BasePath = None }
    
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
                
    let removeBook (a: Audiobook.Audiobook) (l: Library) : Library =
        let updatedBooks = l.Audiobooks |> List.filter (fun book -> book.Id <> a.Id)
        { l with Audiobooks = updatedBooks }
                
    let tryFind (predicate: Audiobook.Audiobook -> bool) (l: Library) : Audiobook.Audiobook option =
        l.Audiobooks |> List.tryFind predicate
        
    let tryFindById (id: int) =
        tryFind (fun (a: Audiobook.Audiobook) -> a.Id = id)
        
    let findById (id: int) =
        (tryFindById id) >> (function Some a -> Ok a | None -> Error "Audiobook with the given id does not exist.")
        
    /// Reads a text file and deserializes a `Library` instance.
    /// Returns errors if the given file does not exist, is not readable or the json is invalid.
    let fromFile filename : Result<Library, string> =
        result {
            let! content = IO.readTextFromFile filename
            return! deserialize content
        }
