module AlNeda.DomainLogic.Tests.OrderWorkflowTests

open Xunit
open AlNeda.DomainLogic
open AlNeda.DomainLogic.OrderWorkflow

[<Fact>]
let ``tryDeductItem deducts when stock sufficient`` () =
    let req =
        { DeductionRequest.ProductId = 1
          DeductionRequest.Quantity = 5
          DeductionRequest.CurrentStock = 10
          DeductionRequest.ProductName = "Test" }
    let result = tryDeductItem req
    match result with
    | Deducted (id, newStock) ->
        Assert.Equal(1, id)
        Assert.Equal(5, newStock)
    | _ -> Assert.Fail("Expected Deducted")

[<Fact>]
let ``tryDeductItem fails when stock insufficient`` () =
    let req =
        { DeductionRequest.ProductId = 1
          DeductionRequest.Quantity = 15
          DeductionRequest.CurrentStock = 10
          DeductionRequest.ProductName = "Test" }
    let result = tryDeductItem req
    match result with
    | InsufficientStock (_, name, avail, requested) ->
        Assert.Equal("Test", name)
        Assert.Equal(10, avail)
        Assert.Equal(15, requested)
    | _ -> Assert.Fail("Expected InsufficientStock")

[<Fact>]
let ``validateTransition allows valid transitions`` () =
    let result = validateTransition "pending" "reviewed"
    match result with
    | Allowed (status, _) -> Assert.Equal("reviewed", status)
    | _ -> Assert.Fail("Expected Allowed")

[<Fact>]
let ``validateTransition denies invalid transitions`` () =
    let result = validateTransition "delivered" "pending"
    match result with
    | Denied _ -> Assert.True(true)
    | _ -> Assert.Fail("Expected Denied")

[<Fact>]
let ``computeFinalTotal with value discount`` () =
    let result = computeFinalTotal 100m 10m "value"
    Assert.Equal(90m, result)

[<Fact>]
let ``computeFinalTotal with percentage discount`` () =
    let result = computeFinalTotal 200m 10m "percent"
    Assert.Equal(180m, result)

[<Fact>]
let ``computeFinalTotal handles negative by returning zero`` () =
    let result = computeFinalTotal 50m 100m "value"
    Assert.Equal(0m, result)

[<Fact>]
let ``updateBalance order increases balance`` () =
    let result = updateBalance 100m 50m "order"
    Assert.Equal(150m, result)

[<Fact>]
let ``updateBalance payment decreases balance`` () =
    let result = updateBalance 100m 50m "payment"
    Assert.Equal(50m, result)

[<Fact>]
let ``updateBalance return decreases balance`` () =
    let result = updateBalance 100m 30m "return"
    Assert.Equal(70m, result)

[<Fact>]
let ``updateBalance never goes below zero`` () =
    let result = updateBalance 20m 100m "payment"
    Assert.Equal(0m, result)

[<Fact>]
let ``remainingAmount computes correctly`` () =
    Assert.Equal(30m, remainingAmount 100m 70m)
    Assert.Equal(0m, remainingAmount 100m 150m)
