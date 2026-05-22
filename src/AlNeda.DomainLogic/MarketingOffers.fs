module AlNeda.DomainLogic.MarketingOffers

open System

let published = "published"
let paused = "paused"
let cancelled = "cancelled"
let depleted = "depleted"
let draft = "draft"
let scheduled = "scheduled"
let review = "review"
let ended = "ended"

let private normalizeOr (fallback: string) (value: string) =
    if String.IsNullOrWhiteSpace value then fallback else value.Trim().ToLowerInvariant()

let normalizeStatus (value: string) =
    match normalizeOr draft value with
    | "draft" -> draft
    | "scheduled" -> scheduled
    | "review" -> review
    | "published" -> published
    | "paused" -> paused
    | "ended" -> ended
    | "cancelled" -> cancelled
    | "depleted" -> depleted
    | _ -> draft

let normalizeOfferType (value: string) =
    match normalizeOr "discount" value with
    | "discount"
    | "bundle"
    | "clearance"
    | "targeted"
    | "new_arrival" as offerType -> offerType
    | _ -> "discount"

let normalizeAudienceRule (value: string) =
    let rule = if String.IsNullOrWhiteSpace value then "all" else value.Trim()
    if rule.Equals("all", StringComparison.OrdinalIgnoreCase)
       || rule.Equals("selected_pharmacies", StringComparison.OrdinalIgnoreCase)
       || rule.Equals("active_pharmacies_last_30d", StringComparison.OrdinalIgnoreCase)
       || rule.StartsWith("geo:", StringComparison.OrdinalIgnoreCase) then
        rule
    else
        "all"

let isPublishedStatus (status: string) =
    (normalizeStatus status) = published

let isActive (status: string) (startsAt: DateTime) (endsAt: DateTime) (remainingQuantity: Nullable<int>) (now: DateTime) =
    isPublishedStatus status
    && startsAt <= now
    && now < endsAt
    && ((not remainingQuantity.HasValue) || remainingQuantity.GetValueOrDefault() > 0)

let eventDateKey (occurredAt: DateTime) =
    occurredAt.ToString("yyyy-MM-dd")

let deviceKey (deviceId: string) =
    if String.IsNullOrWhiteSpace deviceId then "pharmacy" else deviceId.Trim()

let usesDeviceFallback (deviceId: string) =
    String.IsNullOrWhiteSpace deviceId

let validateSchedule (startsAt: DateTime) (endsAt: DateTime) =
    if endsAt <= startsAt then
        "تاريخ نهاية العرض يجب أن يكون بعد تاريخ البداية"
    else
        String.Empty

let validatePrices (oldPrice: Nullable<decimal>) (newPrice: Nullable<decimal>) (discountPercent: Nullable<int>) =
    if oldPrice.HasValue && oldPrice.Value < 0m then
        "السعر قبل الخصم لا يمكن أن يكون أقل من صفر"
    elif newPrice.HasValue && newPrice.Value < 0m then
        "السعر بعد الخصم لا يمكن أن يكون أقل من صفر"
    elif oldPrice.HasValue && newPrice.HasValue && oldPrice.Value > 0m && newPrice.Value > oldPrice.Value then
        "السعر بعد الخصم لا يمكن أن يكون أكبر من السعر قبل الخصم"
    elif discountPercent.HasValue && (discountPercent.Value < 0 || discountPercent.Value > 100) then
        "نسبة الخصم يجب أن تكون بين 0 و 100"
    else
        String.Empty

let validateQuantity (quantityLimit: Nullable<int>) (remainingQuantity: Nullable<int>) =
    if quantityLimit.HasValue && quantityLimit.Value < 0 then
        "حد الكمية لا يمكن أن يكون أقل من صفر"
    elif remainingQuantity.HasValue && remainingQuantity.Value < 0 then
        "الكمية المتبقية لا يمكن أن تكون أقل من صفر"
    elif quantityLimit.HasValue && remainingQuantity.HasValue && remainingQuantity.Value > quantityLimit.Value then
        "الكمية المتبقية لا يمكن أن تكون أكبر من حد الكمية"
    else
        String.Empty

let calculateDiscountPercent (oldPrice: decimal) (newPrice: decimal) =
    if oldPrice <= 0m || newPrice < 0m || newPrice >= oldPrice then
        0
    else
        let percentage = (1m - (newPrice / oldPrice)) * 100m
        int (Math.Round(percentage, MidpointRounding.AwayFromZero))

let nextStatusAfterQuantity (requestedStatus: string) (remainingQuantity: Nullable<int>) =
    if remainingQuantity.HasValue && remainingQuantity.Value = 0 then depleted else normalizeStatus requestedStatus
