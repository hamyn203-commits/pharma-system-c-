module AlNeda.DomainLogic.Tests.DomainTests

open Xunit
open AlNeda.DomainLogic

[<Fact>]
let ``OrderStatus.tryTransition allows valid transitions`` () =
    let result = Domain.OrderStatus.tryTransition "pending" "reviewed"
    Assert.True(result.IsSome)
    Assert.Equal("مراجعة الطلب", result.Value)

[<Fact>]
let ``OrderStatus.tryTransition rejects invalid transitions`` () =
    let result = Domain.OrderStatus.tryTransition "delivered" "pending"
    Assert.True(result.IsNone)

[<Fact>]
let ``OrderStatus.tryTransition allows cancellation before delivery`` () =
    let result = Domain.OrderStatus.tryTransition "pending" "cancelled"
    Assert.True(result.IsSome)

[<Fact>]
let ``OrderStatus.tryTransition rejects cancellation after delivery`` () =
    let result = Domain.OrderStatus.tryTransition "delivered" "cancelled"
    Assert.True(result.IsNone)

[<Fact>]
let ``OrderStatus.validNextStatuses returns expected for pending`` () =
    let next = Domain.OrderStatus.validNextStatuses "pending" |> Set.ofList
    Assert.True(next |> Set.contains "reviewed")
    Assert.True(next |> Set.contains "cancelled")
    Assert.Equal(2, next.Count)

[<Fact>]
let ``OrderStatus.isTerminal identifies terminal states`` () =
    Assert.True(Domain.OrderStatus.isTerminal "delivered")
    Assert.True(Domain.OrderStatus.isTerminal "cancelled")
    Assert.False(Domain.OrderStatus.isTerminal "pending")

[<Fact>]
let ``PaymentStatus.calculate returns unpaid for zero amount`` () =
    let result = Domain.PaymentStatus.calculate 0m 100m "cash"
    Assert.Equal("unpaid", result)

[<Fact>]
let ``PaymentStatus.calculate returns full for paid amount`` () =
    let result = Domain.PaymentStatus.calculate 100m 100m "cash"
    Assert.Equal("full", result)

[<Fact>]
let ``PaymentStatus.calculate returns partial for partial payment`` () =
    let result = Domain.PaymentStatus.calculate 50m 100m "cash"
    Assert.Equal("partial", result)

[<Fact>]
let ``PaymentStatus.calculate returns deferred for deferred type`` () =
    let result = Domain.PaymentStatus.calculate 0m 100m "deferred"
    Assert.Equal("deferred", result)

[<Fact>]
let ``LegacyOrderStatus.normalize maps approved to reviewed`` () =
    Assert.Equal("reviewed", Domain.LegacyOrderStatus.normalize "approved")

[<Fact>]
let ``LegacyOrderStatus.normalize passes through unknown statuses`` () =
    Assert.Equal("pending", Domain.LegacyOrderStatus.normalize "pending")

[<Fact>]
let ``PaymentType.isValid validates correctly`` () =
    Assert.True(Domain.PaymentType.isValid "cash")
    Assert.True(Domain.PaymentType.isValid "deferred")
    Assert.False(Domain.PaymentType.isValid "invalid")
