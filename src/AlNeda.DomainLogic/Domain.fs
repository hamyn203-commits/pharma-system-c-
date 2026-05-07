module AlNeda.DomainLogic.Domain

open System

/// Valid order status transitions
module OrderStatus =

    let allValid =
        [| "pending"; "reviewed"; "in_store"; "with_driver"; "on_the_way"
           "delivered"; "postponed"; "cancelled" |]

    let private normalize (s: string) : string =
        if String.IsNullOrEmpty s then "pending" else s.ToLowerInvariant()

    /// Defines valid transitions for the order state machine.
    let tryTransition (current: string) (next: string) : string option =
        match normalize current, normalize next with
        | "pending",     "reviewed"   -> Some "مراجعة الطلب"
        | "pending",     "cancelled"  -> Some "إلغاء الطلب"
        | "reviewed",    "in_store"   -> Some "تجهيز في المخزن"
        | "in_store",    "with_driver" -> Some "تسليم للمندوب"
        | "with_driver", "on_the_way" -> Some "في الطريق"
        | "on_the_way",  "delivered"  -> Some "تم التوصيل"
        | "delivered",   "postponed"  -> Some "تأجيل الطلب"
        | "postponed",   "on_the_way" -> Some "إعادة في الطريق"
        | "postponed",   "delivered"  -> Some "تم التوصيل بعد التأجيل"
        | "on_the_way",  "postponed"  -> Some "تأجيل أثناء التوصيل"
        | _, "cancelled" when normalize current <> "delivered" && normalize current <> "cancelled" ->
            Some "إلغاء الطلب"
        | _, _ -> None

    /// Returns all valid next statuses for a given current status
    let validNextStatuses (current: string) : string list =
        allValid
        |> Array.filter (fun next -> tryTransition current next |> Option.isSome)
        |> Array.toList

    /// Checks if a status represents a terminal (final) state
    let isTerminal (status: string) : bool =
        match normalize status with
        | "delivered" | "cancelled" -> true
        | _ -> false


/// Payment type validation
module PaymentType =

    let validTypes =
        [| "cash"; "partial"; "full"; "deferred"; "collect_on_delivery" |]

    let isValid (t: string) : bool =
        if String.IsNullOrEmpty t then false
        else validTypes |> Array.contains (t.ToLowerInvariant())


/// Payment status calculation
module PaymentStatus =

    let calculate (amountPaid: decimal) (totalDue: decimal) (paymentType: string) : string =
        let paid = max amountPaid 0m
        let due = max totalDue 0m
        let ptype =
            if String.IsNullOrEmpty paymentType then "cash"
            else paymentType.ToLowerInvariant()

        if (ptype = "deferred" || ptype = "collect_on_delivery") && paid <= 0m then ptype
        elif paid <= 0m then "unpaid"
        elif paid + 0.0001m >= due then "full"
        else "partial"


/// Legacy order status mapping (from the old Python system)
module LegacyOrderStatus =

    let private legacyMap =
        System.Collections.Generic.Dictionary<_, _>()
        |> fun d -> d.Add("approved", "reviewed"); d.Add("rejected", "cancelled"); d.Add("completed", "delivered"); d

    let normalize (status: string) : string =
        let s = if String.IsNullOrEmpty status then "pending" else status.ToLowerInvariant()
        match legacyMap.TryGetValue s with
        | true, v -> v
        | _ -> s
