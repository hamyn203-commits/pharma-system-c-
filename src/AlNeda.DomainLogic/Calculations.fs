module AlNeda.DomainLogic.Calculations

open System

/// Line item in an account statement ledger
type LedgerEntry =
    { Date: System.DateTime
      Reference: string
      Description: string
      Debit: decimal
      Credit: decimal
      RunningBalance: decimal }

type LedgerSource =
    | OrderEntry of orderId: int * amount: decimal * date: System.DateTime
    | PaymentEntry of paymentId: int * amount: decimal * date: System.DateTime
    | ReturnEntry of returnId: int * amount: decimal * date: System.DateTime

/// Builds a running balance ledger from chronological transactions.
/// Orders increase balance (debit), Payments/Returns decrease (credit).
let buildLedger (startingBalance: decimal) (entries: LedgerSource list) : LedgerEntry list =
    entries
    |> List.sortBy (fun e ->
        match e with
        | OrderEntry (_, _, d) -> d
        | PaymentEntry (_, _, d) -> d
        | ReturnEntry (_, _, d) -> d)
    |> List.fold (fun (balance, acc) entry ->
        match entry with
        | OrderEntry (id, amount, date) ->
            let newBalance = balance + amount
            let le =
                { Date = date
                  Reference = $"طلب #{id}"
                  Description = "فاتورة طلب"
                  Debit = amount
                  Credit = 0m
                  RunningBalance = newBalance }
            newBalance, le :: acc
        | PaymentEntry (id, amount, date) ->
            let newBalance = max (balance - amount) 0m
            let le =
                { Date = date
                  Reference = $"دفع #{id}"
                  Description = "تحصيل مدفوعات"
                  Debit = 0m
                  Credit = amount
                  RunningBalance = newBalance }
            newBalance, le :: acc
        | ReturnEntry (id, amount, date) ->
            let newBalance = max (balance - amount) 0m
            let le =
                { Date = date
                  Reference = $"مرتجع #{id}"
                  Description = "مرتجع مشتريات"
                  Debit = 0m
                  Credit = amount
                  RunningBalance = newBalance }
            newBalance, le :: acc)
        (startingBalance, [])
    |> snd
    |> List.rev

/// Calculates totals for dashboard summary
type DashboardSummary =
    { TotalProducts: int
      TotalPharmacies: int
      ActiveOrders: int
      TotalOutstandingBalance: decimal
      TodaySales: decimal
      ThisMonthSales: decimal }

/// Aggregates payment information for a pharmacy
type PharmacyAccountSummary =
    { PharmacyId: int
      PharmacyName: string
      CurrentBalance: decimal
      TotalOrders: decimal
      TotalPayments: decimal
      TotalReturns: decimal
      LastTransactionDate: System.DateTime option }

/// Calculates the total outstanding balance from a set of pharmacy balances
let totalOutstanding (pharmacyBalances: decimal seq) : decimal =
    pharmacyBalances |> Seq.filter (fun b -> b > 0m) |> Seq.sum
