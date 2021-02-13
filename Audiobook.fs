namespace b0wter.Fbrary

open System
open b0wter.Fbrary.Utilities
open b0wter.FSharp

module Audiobook =
    
    type AudiobookSource
        = MultiFile of string list
        | SingleFile of string
        
    let sourceAsString = function
        | MultiFile files -> (files |> List.head) + (sprintf " + %i more" (files.Length - 1))
        | SingleFile file -> file
        
    type Audiobook = {
        Id: int
        Source: AudiobookSource
        Artist: string option
        Album: string option
        AlbumArtist: string option
        Title: string option
        Duration: TimeSpan
        HasPicture: bool
        Comment: string option
        Rating: int option
        Completed: bool
    }
    
    //
    // Functions for accessing the properties of an audiobook.
    //
    let id a = a.Id
    let source a = a.Source
    let artists a = a.Artist
    let album a = a.Album
    let title a = a.Title
    let duration a = a.Duration
    let hasPicture a = a.HasPicture
    let comment a = a.Comment
    let rating a = a.Rating
    let completed a = a.Completed
    
    //
    // Functions for updating the properties of an audiobook.
    //
    let withRating (rating: int) (a: Audiobook) : Audiobook =
        { a with Rating = Some rating }
        
    let withComment (comment: string) (a: Audiobook) : Audiobook =
        { a with Comment = Some comment }
        
    let withId (id: int) (a: Audiobook) : Audiobook =
        { a with Id = id }
        
    //
    // Other functions.
    //
    let createWith source artists album albumArtist title duration hasPicture comment (idGenerator: unit -> int) rating =
        {
            Id = idGenerator ()
            Source = source
            Artist = artists
            Album = album
            AlbumArtist = albumArtist
            Title = title
            Duration = duration
            HasPicture = hasPicture
            Comment = comment
            Rating = rating
            Completed = false
        }
    
    let createInteractive source artist album albumArtist title duration hasPicture comment (idGenerator: unit -> int) =
        // TODO: This code works but not in a good way. Needs refactoring.
        let filename = match source with SingleFile f -> f | MultiFile m -> m |> List.tryHead |> Option.getOrElse "<no filename>"
        let readFromInput (fieldName: string) (value: string option) : string option =
            let nonOptionValue = match value with Some s -> s | None -> String.Empty
            match nonOptionValue with
            | s when String.IsNullOrWhiteSpace(s) ->
                do printfn "The field '%s' is empty. Do you want to set it? [y/N]" fieldName
                let key = Console.ReadKey true
                if key.Key = ConsoleKey.Y || key.KeyChar = 'y' || key.KeyChar = 'Y' then
                    Console.Write("Please enter the new value: ")
                    Console.ReadLine() |> Some
                else
                    value
            | s ->
                do printfn "The field '%s' has the value: '%s'. Is that correct? [Y/n]" fieldName s
                let key = Console.ReadKey true
                if key.Key = ConsoleKey.N || key.KeyChar = 'n' || key.KeyChar = 'N' then
                    Console.Write("Please enter the new value: ")
                    Console.ReadLine() |> Some
                else
                    value
                    
        let ratingFromInput () : int option =
            let mutable breaker = true
            let mutable value = None
            do printfn "Do you want to add a rating? [y/N]"
            if Console.ReadKey(true).Key = ConsoleKey.Y then
                do printfn "Please enter a value from 1 to 10."
                while breaker do
                    match Parsers.parseInt(Console.ReadLine()) with
                    | Some i ->
                        do breaker <- false
                        do value <- Some i
                    | None ->
                        printfn "Please enter a value from 1 to 10."
                        ()
                value
            else
                None
        
        do printfn "Updating metadata for '%s'." filename
        let artist = readFromInput "Artist" artist
        let album = readFromInput "Album" album
        let albumArtist = readFromInput "Album Artist" albumArtist
        let title = readFromInput "Title" title
        let comment = readFromInput "Comment" comment
        let rating = ratingFromInput ()
                
        createWith source artist album albumArtist title duration hasPicture comment idGenerator rating
        
    let createWithPossibleInteraction (config: Config.AddConfig) source artist album albumArtist title duration hasPicture comment (idGenerator: unit -> int) =
        if config.NonInteractive then
            createWith source artist album albumArtist title duration hasPicture comment idGenerator None
        else
            createInteractive source artist album albumArtist title duration hasPicture comment idGenerator
        
    /// Creates an audiobook for a single file.
    let createFromSingle config idGenerator (m: Metadata.Metadata) =
        createWithPossibleInteraction
            config
            (SingleFile m.Filename)
            m.Artist
            m.Album
            m.AlbumArtist
            m.Title
            m.Duration
            m.HasPicture
            m.Comment
            idGenerator
        
    /// Creates an audiobook for a list of files.
    let createFromMultiple config (idGenerator: unit -> int) (mm: Metadata.Metadata list) : Result<Audiobook, string> =
        // If multiple input files are given there is no proper way to
        // select the matching title if there is more than one.
        let selectedTitle =
            let distinct = mm |> List.map (fun m -> m.Title) |> List.choose FSharp.Core.Operators.id |> List.distinct
            if distinct.Length > 1 then None
            else distinct |> List.tryHead
            
        match mm with
        | [] -> Error "Cannot create an audiobook from an empty source."
        | head :: _ ->
            let duration = mm |> List.fold (fun (aggregator: TimeSpan) (next: Metadata.Metadata) -> aggregator.Add(next.Duration)) TimeSpan.Zero
            createWithPossibleInteraction
                config
                (mm |> List.map (fun m -> m.Filename) |> MultiFile)
                head.Artist
                head.Album
                head.AlbumArtist
                selectedTitle 
                duration
                head.HasPicture
                head.Comment
                idGenerator
            |> Ok
        
    let properties (a: Audiobook) =
        [ a.Album; a.Artist; a.Title; a.AlbumArtist ] |> List.choose FSharp.Core.Operators.id
    
    let maxPropertyLength (a: Audiobook) =
        a
        |> properties
        |> List.map (string >> String.length)
        |> (fun list -> if list.IsEmpty then 0 else list |> List.max)

    let serialize (a: Audiobook) : string =
        Microsoft.FSharpLu.Json.Default.serialize a
        
    let deserialize (jsonString: string) : Result<Audiobook, string> =
        let result = Microsoft.FSharpLu.Json.Default.tryDeserialize<Audiobook> jsonString
        match result with
        | Choice1Of2 a -> Ok a
        | Choice2Of2 e -> Error e
        
    let containsString pattern (a: Audiobook) =
        a |> properties |> List.exists (String.contains pattern)
        
    let isSameSource (a: Audiobook) (b: Audiobook) : bool =
        match a.Source, b.Source with
        | SingleFile _, MultiFile _ -> false
        | MultiFile _, SingleFile _ -> false
        | SingleFile s1, SingleFile s2 -> s1 = s2
        | MultiFile m1, MultiFile m2 -> m1 = m2
        