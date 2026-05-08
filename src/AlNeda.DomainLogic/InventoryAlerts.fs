module AlNeda.DomainLogic.Inventory

open System

/// Inventory alert thresholds
type InventoryThresholds = {
    LowStockThreshold: int
    ExpiryWarningDays: int
    ExpiryCriticalDays: int
}

/// Inventory alert types
type AlertType =
    | LowStock
    | ExpiringWarning
    | ExpiringCritical
    | Expired

/// Product inventory alert
type ProductAlert = {
    ProductId: int
    ProductName: string
    AlertType: AlertType
    CurrentQuantity: int
    DaysUntilExpiry: int option
    Threshold: int
    Message: string
}

/// Inventory status
type InventoryStatus = {
    TotalProducts: int
    LowStockCount: int
    ExpiringCount: int
    ExpiredCount: int
    CriticalCount: int
}

/// Creates default thresholds
let createDefaultThresholds () = {
    LowStockThreshold = 10
    ExpiryWarningDays = 30
    ExpiryCriticalDays = 7
}

/// Parses thresholds from configuration
let parseThresholds (config: Map<string, obj>) : InventoryThresholds =
    let tryGetInt key defaultVal =
        config
        |> Map.tryFind key
        |> Option.map (fun v -> match v with :? int as i -> i | _ -> defaultVal)
        |> Option.defaultValue defaultVal

    {
        LowStockThreshold = tryGetInt "LowStockThreshold" 10
        ExpiryWarningDays = tryGetInt "ExpiryWarningDays" 30
        ExpiryCriticalDays = tryGetInt "ExpiryCriticalDays" 7
    }

/// Checks if a product has low stock
let isLowStock (quantity: int) (threshold: int) : bool =
    quantity < threshold && quantity > 0

/// Checks if stock is completely depleted
let isOutOfStock (quantity: int) : bool =
    quantity <= 0

/// Calculates days until expiry
let daysUntilExpiry (expiryDateStr: string option) : int option =
    match expiryDateStr with
    | None -> None
    | Some dateStr ->
        match DateTime.TryParse(dateStr) with
        | true, date -> Some (date.Subtract(DateTime.Today).Days)
        | false, _ -> None

/// Determines alert type based on days until expiry
let getExpiryAlertType (daysUntilExpiry: int) (warningDays: int) (criticalDays: int) : AlertType option =
    if daysUntilExpiry < 0 then Some Expired
    elif daysUntilExpiry <= criticalDays then Some ExpiringCritical
    elif daysUntilExpiry <= warningDays then Some ExpiringWarning
    else None

/// Creates an alert message
let createAlertMessage (productName: string) (alertType: AlertType) (quantityOrDays: int) (threshold: int) : string =
    match alertType with
    | LowStock ->
        if quantityOrDays <= 0 then
            $"المنتج '{productName}'已经完全缺货！需要立即补充库存。"
        else
            $"المنتج '{productName}' مخزونه منخفض ({quantityOrDays} وحدة). الحد الأدنى: {threshold}"
    | ExpiringWarning -> $"المنتج '{productName}' سينتهي خلال {quantityOrDays} يوم"
    | ExpiringCritical -> $"المنتج '{productName}' سينتهي قريباً خلال {quantityOrDays} يوم - عاجل!"
    | Expired -> $"المنتج '{productName}' منتهي الصلاحية!"

/// Analyzes a single product and returns alerts
let analyzeProduct (thresholds: InventoryThresholds) (productId: int) (productName: string) (quantity: int) (expiryDateStr: string option) : ProductAlert list =
    let alerts = ResizeArray<ProductAlert>()

    // Check low stock
    if isLowStock quantity thresholds.LowStockThreshold then
        let alertType = if isOutOfStock quantity then Expired else LowStock
        let message = createAlertMessage productName alertType quantity thresholds.LowStockThreshold
        alerts.Add({
            ProductId = productId
            ProductName = productName
            AlertType = alertType
            CurrentQuantity = quantity
            DaysUntilExpiry = None
            Threshold = thresholds.LowStockThreshold
            Message = message
        })

    // Check expiry
    match daysUntilExpiry expiryDateStr with
    | Some days ->
        match getExpiryAlertType days thresholds.ExpiryWarningDays thresholds.ExpiryCriticalDays with
        | Some alertType ->
            let threshold = if alertType = ExpiringCritical then thresholds.ExpiryCriticalDays else thresholds.ExpiryWarningDays
            let message = createAlertMessage productName alertType days threshold
            alerts.Add({
                ProductId = productId
                ProductName = productName
                AlertType = alertType
                CurrentQuantity = quantity
                DaysUntilExpiry = Some days
                Threshold = threshold
                Message = message
            })
        | None -> ()
    | None -> ()

    List.ofSeq alerts

/// Analyzes multiple products and returns all alerts
let analyzeInventory (thresholds: InventoryThresholds) (products: (int * string * int * string option) seq) : ProductAlert list =
    products
    |> Seq.collect (fun (id, name, qty, expiry) ->
        analyzeProduct thresholds id name qty expiry)
    |> Seq.sortBy (fun alert ->
        // Sort by urgency: expired first, then critical, warning, low stock
        match alert.AlertType with
        | Expired -> 0
        | ExpiringCritical -> 1
        | ExpiringWarning -> 2
        | LowStock -> 3)
    |> Seq.toList

/// Groups alerts by type
let groupAlertsByType (alerts: ProductAlert list) : Map<AlertType, ProductAlert list> =
    alerts
    |> List.groupBy (fun a -> a.AlertType)
    |> Map.ofList

/// Calculates inventory status summary
let calculateInventoryStatus (products: (int * string * int * string option) seq) (thresholds: InventoryThresholds) : InventoryStatus =
    let mutable totalProducts = 0
    let mutable lowStockCount = 0
    let mutable expiringCount = 0
    let mutable expiredCount = 0
    let mutable criticalCount = 0

    products |> Seq.iter (fun (_, _, qty, expiry) ->
        totalProducts <- totalProducts + 1

        if isLowStock qty thresholds.LowStockThreshold then
            lowStockCount <- lowStockCount + 1

        match daysUntilExpiry expiry with
        | Some days ->
            if days < 0 then expiredCount <- expiredCount + 1
            elif days <= thresholds.ExpiryCriticalDays then criticalCount <- criticalCount + 1
            elif days <= thresholds.ExpiryWarningDays then expiringCount <- expiringCount + 1
        | None -> ()
    )

    {
        TotalProducts = totalProducts
        LowStockCount = lowStockCount
        ExpiringCount = expiringCount
        ExpiredCount = expiredCount
        CriticalCount = criticalCount
    }

/// Filters products by alert type
let filterByAlertType (alertType: AlertType) (alerts: ProductAlert list) : ProductAlert list =
    alerts |> List.filter (fun a -> a.AlertType = alertType)

/// Gets top N most urgent alerts
let getTopAlerts (count: int) (alerts: ProductAlert list) : ProductAlert list =
    alerts |> List.take (min count alerts.Length)

/// Calculates alert priority score (higher = more urgent)
let calculatePriorityScore (alert: ProductAlert) : int =
    match alert.AlertType with
    | Expired -> 1000 + abs(alert.DaysUntilExpiry |> Option.defaultValue 0)
    | ExpiringCritical -> 500 + (100 - min (alert.DaysUntilExpiry |> Option.defaultValue 0) 100)
    | ExpiringWarning -> 200 + (30 - min (alert.DaysUntilExpiry |> Option.defaultValue 30) 30)
    | LowStock ->
        if alert.CurrentQuantity <= 0 then 300
        else 100 - min (alert.CurrentQuantity * 10) 100

/// Sorts alerts by priority
let sortByPriority (alerts: ProductAlert list) : ProductAlert list =
    alerts |> List.sortByDescending calculatePriorityScore