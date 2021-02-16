namespace b0wter.Fbrary

module Rating =
    type Rating = Rating of int
    
    let minValue = 1
    let maxValue = 5
    
    let create success failure (i: int) =
        if i >= minValue && i <= maxValue then success (Rating i) 
        elif i >= minValue then failure (sprintf "The rating must be less or equal to %i." maxValue)
        elif i <= maxValue then failure (sprintf "The rating must be greater or equal to %i." minValue)
        else failure (sprintf "The rating must be greater or equal to %i and less or equal to %i" minValue maxValue)
        
    let value (Rating r) = r
