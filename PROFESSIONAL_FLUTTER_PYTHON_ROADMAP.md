# خطة إدخال Flutter وPython في مشروع مخزن الندى

## الهدف

نخلي المشروع احترافي من غير ما نرمي الشغل الموجود:

- **.NET API** يفضل هو المصدر الأساسي للبيانات والعمليات الحساسة.
- **Flutter Desktop** يبقى واجهة الإدارة الجديدة تدريجيا بدل WPF.
- **Python FastAPI** يبقى طبقة ذكاء وتحليلات وتوصيات، مثل توقع المبيعات وتنبيهات إعادة الطلب.

## اللي اتضاف

- `python_analytics_service/`: خدمة Python منفصلة للـ analytics.
- `lib/core/config/app_config.dart`: إعدادات روابط الـ API.
- `lib/core/network/api_client.dart`: Dio client مع JWT storage.
- `lib/features/auth/data/auth_api.dart`: بداية ربط تسجيل الدخول.
- `lib/features/dashboard/data/dashboard_api.dart`: بداية ربط Dashboard مع .NET API وPython insights.
- `Start-Professional.bat`: تشغيل .NET API + Python service + Flutter app.

## المسار المقترح

1. تجهيز Flutter platform files وتشغيل نسخة Windows.
2. إضافة Login screen في Flutter وربطه بـ `/api/auth/login`.
3. تحويل Dashboard من بيانات تجريبية إلى بيانات حقيقية من:
   - `.NET`: `/api/dashboard/stats`
   - `.NET`: `/api/dashboard/sales/last30`
   - `Python`: `/insights/dashboard`
4. نقل الشاشات الأهم واحدة واحدة إلى Flutter:
   - المنتجات
   - الصيدليات
   - الطلبات
   - المدفوعات
   - التقارير
5. تطوير Python analytics:
   - توصيات شراء ذكية حسب سرعة البيع.
   - توقع مبيعات الأسبوع القادم.
   - كشف المنتجات الراكدة.
   - تنبيهات انتهاء الصلاحية.

## طريقة التشغيل الجديدة

```bat
Start-Professional.bat
```

الخدمة الجديدة تعمل افتراضيا على:

- .NET API: `http://localhost:5000`
- Python analytics: `http://localhost:8010`
- Flutter Desktop: Windows runner

## ملاحظات مهمة

- Python service حاليا read-only ولا تعدل في قاعدة البيانات.
- قاعدة البيانات الافتراضية هي `pharmacy.db` في جذر المشروع.
- يمكن تغيير مسار قاعدة البيانات عبر `ALNEDA_DB_PATH`.
