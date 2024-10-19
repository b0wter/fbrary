namespace Fbrary.Cli

open System
open FsToolkit.ErrorHandling
open b0wter.FSharp.Collections
open Fbrary.Core.Utilities
open Fbrary.Core

module Library =
    
    type Library = {
        Audiobooks: Audiobook.Audiobook list
        LastUpdated: DateTime
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

    let empty = { Audiobooks = []; LastUpdated = DateTime.MinValue; BasePath = None }
    
    let addTo (l: Library) (a: Audiobook.Audiobook) : Result<Library, string> =
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
                        LastUpdated = DateTime.Now
                } |> Ok
        | None ->
            if l.Audiobooks |> List.map Audiobook.id |> List.contains a.Id then Error "The audiobook could not be added to the library because its id is already taken."
            else
                {
                    l with
                        Audiobooks = a :: l.Audiobooks
                        LastUpdated = DateTime.Now
                } |> Ok
                
    let addBook a l = addTo l a

    let updateBooks (aa: Audiobook.Audiobook list) (l: Library) : Result<Library, string> =
        let exempted = aa |> List.map Audiobook.id
        let predicate = fun (a: Audiobook.Audiobook) -> exempted |> List.contains a.Id
        let splitBooks = List.splitBy predicate l.Audiobooks
        let mergedBooks = aa @ splitBooks.NonMatching
        Ok { l with Audiobooks = mergedBooks }
        
    let updateBook (a: Audiobook.Audiobook) (l: Library) : Result<Library, string> =
        match l.Audiobooks |> List.tryFind (fun b -> b.Id = a.Id) with
        | Some book ->
            Ok { l with Audiobooks = (List.replace book a l.Audiobooks) }
        | None -> Error (sprintf "An audio book with the id '%i' does not exist." a.Id)
        
    let removeBook (a: Audiobook.Audiobook) (l: Library) : Library =
        let updatedBooks = l.Audiobooks |> List.filter (fun book -> book.Id <> a.Id)
        { l with Audiobooks = updatedBooks }
                
    let tryFind (predicate: Audiobook.Audiobook -> bool) (l: Library) : Audiobook.Audiobook option =
        l.Audiobooks |> List.tryFind predicate
        
    let tryFindById (id: int) =
        tryFind (fun (a: Audiobook.Audiobook) -> a.Id = id)
        
    let findById (id: int) =
        (tryFindById id) >> (function Some a -> Ok a | None -> Error "Audiobook with the given id does not exist.")
        
    let findByIds (ids: int list) (l: Library) =
        let rec step (accumulator: Audiobook.Audiobook list) (remainingIds: int list) (remainingBooks: Audiobook.Audiobook list) =
            match remainingBooks, remainingIds with
            | _, [] -> Ok accumulator
            | [], _ -> Error (sprintf "Could not find the following ids: %s" (remainingIds |> List.map string |> (fun s -> String.Join(", ", s))))
            | head :: tail, _ -> if remainingIds |> List.contains head.Id then
                                     step (head :: accumulator) (remainingIds |> List.remove head.Id) tail
                                 else
                                     step accumulator remainingIds tail
        step [] ids l.Audiobooks
        
    let findBy (predicate: Audiobook.Audiobook -> bool) (l: Library) : Result<Audiobook.Audiobook, string> =
        match l.Audiobooks |> List.tryFind predicate with
        | Some book -> Ok book
        | None -> Error "Found no audio book matching the predicate"
        
    let tryFindBy (predicate: Audiobook.Audiobook -> bool) (l: Library) : Audiobook.Audiobook option =
        l.Audiobooks |> List.tryFind predicate
        
    /// Reads a text file and deserializes a `Library` instance.
    /// Returns errors if the given file does not exist, is not readable or the json is invalid.
    let fromFile filename : Result<Library, string> =
        result {
            let! content = IO.readTextFromFile filename
            return! deserialize content
        }
