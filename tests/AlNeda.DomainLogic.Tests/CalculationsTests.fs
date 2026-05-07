module AlNeda.DomainLogic.Tests.CalculationsTests

open Xunit
open AlNeda.DomainLogic

[<Fact>]
let ``buildLedger produces correct running balance`` () =
    let entries =
        [ Calculations.OrderEntry (1, 100m, System.DateTime(2026, 1, 1))
          Calculations.PaymentEntry (1, 30m, System.DateTime(2026, 1, 2))
          Calculations.ReturnEntry (1, 10m, System.DateTime(2026, 1, 3)) ]

    let ledger = Calculations.buildLedger 0m entries
    Assert.Equal(3, ledger.Length)

    // First entry: order of 100, balance 100
    Assert.Equal(100m, ledger[0].RunningBalance)
    Assert.Equal(100m, ledger[0].Debit)
    Assert.Equal(0m, ledger[0].Credit)

    // Second entry: payment of 30, balance 70
    Assert.Equal(70m, ledger[1].RunningBalance)
    Assert.Equal(0m, ledger[1].Debit)
    Assert.Equal(30m, ledger[1].Credit)

    // Third entry: return of 10, balance 60
    Assert.Equal(60m, ledger[2].RunningBalance)
    Assert.Equal(0m, ledger[2].Debit)
    Assert.Equal(10m, ledger[2].Credit)

[<Fact>]
let ``buildLedger sorts entries by date`` () =
    let entries =
        [ Calculations.PaymentEntry (1, 50m, System.DateTime(2026, 1, 5))
          Calculations.OrderEntry (1, 200m, System.DateTime(2026, 1, 1)) ]

    let ledger = Calculations.buildLedger 0m entries
    Assert.Equal(200m, ledger[0].RunningBalance)  // order first
    Assert.Equal(150m, ledger[1].RunningBalance)  // payment after

[<Fact>]
let ``totalOutstanding sums positive balances`` () =
    let balances = [ 0m; 100m; 50m; 0m; 200m ]
    Assert.Equal(350m, Calculations.totalOutstanding balances)
