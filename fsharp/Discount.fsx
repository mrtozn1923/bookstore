// The same Bookstore rules in F#: records, a discriminated union, composition with >> and |>
type CartLine = { Isbn: string; Title: string; UnitPrice: decimal; Quantity: int; IsEbook: bool }

type OrderStatus =
    | Pending
    | Paid of paymentId: string
    | Shipped of paymentId: string * trackingNo: string
    | Cancelled of reason: string

let percentOff rate amount = amount * (1m - rate)
let amountOff discount amount = max 0m (amount - discount)
let roundToKurus (amount: decimal) = System.Math.Round(amount, 2, System.MidpointRounding.AwayFromZero)

// Coupon first, then the member discount, then rounding
let checkoutRules = percentOff 0.10m >> amountOff 15m >> roundToKurus

let cart =
    [ { Isbn = "9780441013593"; Title = "Dune"; UnitPrice = 189.50m; Quantity = 1; IsEbook = false }
      { Isbn = "9780141439587"; Title = "Emma"; UnitPrice = 59.90m; Quantity = 1; IsEbook = true }
      { Isbn = "9780261103344"; Title = "The Hobbit"; UnitPrice = 149.90m; Quantity = 0; IsEbook = false } ]

let subtotal =
    cart
    |> List.filter (fun line -> line.Quantity > 0)
    |> List.sumBy (fun line -> line.UnitPrice * decimal line.Quantity)

printfn "subtotal: %M, after rules: %M" subtotal (checkoutRules subtotal)

let describe status =
    match status with
    | Pending -> "pending"
    | Paid paymentId -> $"paid with {paymentId}"
    | Shipped (_, trackingNo) -> $"shipped with {trackingNo}"
    | Cancelled reason -> $"cancelled: {reason}"

[ Pending; Paid "PAY-1001"; Shipped("PAY-1001", "TRK-778"); Cancelled "customer request" ]
|> List.iter (describe >> printfn "%s")
