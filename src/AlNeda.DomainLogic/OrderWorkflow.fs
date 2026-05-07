module AlNeda.DomainLogic.OrderWorkflow

open System

/// Domain types for order workflow
type DeductionRequest =
    { ProductId: int
      Quantity: int
      CurrentStock: int
      ProductName: string }

type DeductionResult =
    | Deducted of productId: int * newStock: int
    | InsufficientStock of productId: int * productName: string * available: int * requested: int

type TransitionOutcome =
    | Allowed of newStatus: string * message: string
    | Denied of reason: string

let private normalizeOr (s: string) (fallback: string) : string =
    if System.String.IsNullOrEmpty s then fallback else s.ToLowerInvariant()

/// Attempts stock deduction for a single product.
let tryDeductItem (req: DeductionRequest) : DeductionResult =
    if req.CurrentStock < req.Quantity then
        InsufficientStock(req.ProductId, req.ProductName, req.CurrentStock, req.Quantity)
    else
        Deducted(req.ProductId, req.CurrentStock - req.Quantity)

/// Validates an order status transition using the domain state machine.
let validateTransition (currentStatus: string) (nextStatus: string) : TransitionOutcome =
    let current = normalizeOr currentStatus "pending"
    let next = normalizeOr nextStatus ""
    if System.String.IsNullOrEmpty next then
        Denied "الحالة المطلوبة غير صالحة"
    else
        match Domain.OrderStatus.tryTransition current next with
        | None -> Denied($"لا يمكن تغيير حالة الطلب من '{current}' إلى '{next}'")
        | Some msg -> Allowed(next, msg)

/// Computes final total after discount.
let computeFinalTotal (totalAmount: decimal) (discount: decimal) (discountType: string) : decimal =
    let dt = normalizeOr discountType "value"
    if dt = "percent" || dt = "percentage" then
        let discountAmount = totalAmount * discount / 100m
        max (totalAmount - discountAmount) 0m
    else
        max (totalAmount - discount) 0m

/// Updates pharmacy balance: order increases (debit), payment/return decreases (credit).
let updateBalance (currentBalance: decimal) (amount: decimal) (operation: string) : decimal =
    match normalizeOr operation "" with
    | "order" -> currentBalance + amount
    | "payment"
    | "return" -> max (currentBalance - amount) 0m
    | _ -> currentBalance

/// Computes remaining amount from total and paid.
let remainingAmount (total: decimal) (paid: decimal) : decimal = max (total - paid) 0m
