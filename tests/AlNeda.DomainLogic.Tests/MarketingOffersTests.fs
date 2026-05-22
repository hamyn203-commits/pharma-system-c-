module AlNeda.DomainLogic.Tests.MarketingOffersTests

open System
open Xunit
open AlNeda.DomainLogic.MarketingOffers

[<Fact>]
let ``isActive requires published status and current date window`` () =
    let now = DateTime(2026, 5, 15, 12, 0, 0)
    let startsAt = now.AddHours(-1.0)
    let endsAt = now.AddHours(1.0)

    Assert.True(isActive "published" startsAt endsAt (Nullable<int>()) now)
    Assert.False(isActive "draft" startsAt endsAt (Nullable<int>()) now)
    Assert.False(isActive "published" startsAt now (Nullable<int>()) now)

[<Fact>]
let ``isActive rejects depleted remaining quantity`` () =
    let now = DateTime(2026, 5, 15, 12, 0, 0)
    Assert.False(isActive "published" (now.AddDays(-1.0)) (now.AddDays(1.0)) (Nullable<int>(0)) now)
    Assert.True(isActive "published" (now.AddDays(-1.0)) (now.AddDays(1.0)) (Nullable<int>(3)) now)

[<Fact>]
let ``validatePrices rejects impossible discount setup`` () =
    Assert.Equal(String.Empty, validatePrices (Nullable<decimal>(100m)) (Nullable<decimal>(75m)) (Nullable<int>(25)))
    Assert.False(String.IsNullOrEmpty(validatePrices (Nullable<decimal>(100m)) (Nullable<decimal>(120m)) (Nullable<int>(0))))
    Assert.False(String.IsNullOrEmpty(validatePrices (Nullable<decimal>()) (Nullable<decimal>()) (Nullable<int>(101))))

[<Fact>]
let ``validateQuantity prevents negative and over limit remaining quantity`` () =
    Assert.Equal(String.Empty, validateQuantity (Nullable<int>(10)) (Nullable<int>(4)))
    Assert.False(String.IsNullOrEmpty(validateQuantity (Nullable<int>(10)) (Nullable<int>(11))))
    Assert.False(String.IsNullOrEmpty(validateQuantity (Nullable<int>(-1)) (Nullable<int>(0))))

[<Fact>]
let ``normalizers keep only supported values`` () =
    Assert.Equal("discount", normalizeOfferType "weird")
    Assert.Equal("bundle", normalizeOfferType " Bundle ")
    Assert.Equal("all", normalizeAudienceRule "unknown")
    Assert.Equal("selected_pharmacies", normalizeAudienceRule "selected_pharmacies")
    Assert.Equal("draft", normalizeStatus "bad-status")
    Assert.Equal("published", normalizeStatus " Published ")

[<Fact>]
let ``nextStatusAfterQuantity marks empty offer as depleted`` () =
    Assert.Equal("depleted", nextStatusAfterQuantity "published" (Nullable<int>(0)))
    Assert.Equal("published", nextStatusAfterQuantity "published" (Nullable<int>(2)))
