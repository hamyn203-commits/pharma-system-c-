module AlNeda.DomainLogic.Reporting

open System
open System.Linq

/// Sales report data point
type SalesByDay =
    { Date: System.DateTime
      OrderCount: int
      TotalSales: decimal
      TotalDiscounts: decimal }

/// Top product ranking
type ProductRanking =
    { ProductId: int
      ProductName: string
      TotalQuantity: int
      TotalRevenue: decimal }

/// Top pharmacy ranking
type PharmacyRanking =
    { PharmacyId: int
      PharmacyName: string
      OrderCount: int
      TotalPurchases: decimal
      TotalPayments: decimal
      CurrentDebt: decimal }

/// Aggregates order totals by day
let aggregateSalesByDay (orders: (System.DateTime * decimal * decimal) seq) : SalesByDay list =
    orders
    |> Seq.groupBy (fun (date, _, _) -> date.Date)
    |> Seq.map (fun (date, group) ->
        let items = group |> Seq.toList
        { Date = date
          OrderCount = items.Length
          TotalSales = items |> List.sumBy (fun (_, total, _) -> total)
          TotalDiscounts = items |> List.sumBy (fun (_, _, disc) -> disc) })
    |> Seq.sortBy (fun r -> r.Date)
    |> Seq.toList

/// Ranks products by total quantity sold
let rankProductsBySales (items: (int * string * int * decimal) seq) : ProductRanking list =
    items
    |> Seq.groupBy (fun (id, name, _, _) -> id, name)
    |> Seq.map (fun ((id, name), group) ->
        { ProductId = id
          ProductName = name
          TotalQuantity = group |> Seq.sumBy (fun (_, _, qty, _) -> qty)
          TotalRevenue = group |> Seq.sumBy (fun (_, _, _, rev) -> rev) })
    |> Seq.sortByDescending (fun r -> r.TotalRevenue)
    |> Seq.toList

/// Ranks pharmacies by total purchase volume
let rankPharmacies (data: (int * string * int * decimal * decimal * decimal) seq) : PharmacyRanking list =
    data
    |> Seq.map (fun (id, name, oc, tp, tpay, debt) ->
        { PharmacyId = id
          PharmacyName = name
          OrderCount = oc
          TotalPurchases = tp
          TotalPayments = tpay
          CurrentDebt = debt })
    |> Seq.sortByDescending (fun r -> r.TotalPurchases)
    |> Seq.toList
