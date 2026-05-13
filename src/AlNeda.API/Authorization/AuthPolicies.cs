namespace AlNeda.API.Authorization;

/// <summary>
/// سياسات التفويض: مسارات الإدارة (WPF) مقابل تطبيق الصيدلية (Mobile).
/// Staff = admin | accountant | rep — كل ما تحت /api ما عدا /api/mobile ونقاط auth العامة.
/// PharmacyApp = pharmacy فقط — مسار تطبيق الصيدلية تحت /api/mobile (MobileController).
/// AdminOnly = إدارة المستخدمين والعمليات الحساسة.
/// </summary>
public static class AuthPolicies
{
    public const string Staff = "Staff";
    public const string PharmacyApp = "PharmacyApp";
    public const string AdminOnly = "AdminOnly";
}
