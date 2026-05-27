import 'package:al_neda_admin_flutter/core/network/api_client.dart';

final class DashboardApi {
  const DashboardApi(this._client);

  final ApiClient _client;

  Future<DashboardStatsDto> getStats() async {
    final response = await _client.dotnetApi.get<Map<String, dynamic>>(
      '/api/dashboard/stats',
    );
    return DashboardStatsDto.fromJson(response.data ?? {});
  }

  Future<List<SalesDataPointDto>> getLast30DaysSales() async {
    final response = await _client.dotnetApi.get<List<dynamic>>(
      '/api/dashboard/sales/last30',
    );
    return (response.data ?? [])
        .whereType<Map<String, dynamic>>()
        .map(SalesDataPointDto.fromJson)
        .toList(growable: false);
  }

  Future<List<ProductAlertDto>> getLowStockAlerts() {
    return _getProductAlerts('/api/dashboard/alerts/low-stock');
  }

  Future<List<ProductAlertDto>> getExpiringAlerts() {
    return _getProductAlerts('/api/dashboard/alerts/expiring');
  }

  Future<PythonDashboardInsightsDto> getPythonInsights() async {
    final response = await _client.analyticsApi.get<Map<String, dynamic>>(
      '/insights/dashboard',
    );
    return PythonDashboardInsightsDto.fromJson(response.data ?? {});
  }

  Future<List<ProductAlertDto>> _getProductAlerts(String path) async {
    final response = await _client.dotnetApi.get<List<dynamic>>(path);
    return (response.data ?? [])
        .whereType<Map<String, dynamic>>()
        .map(ProductAlertDto.fromJson)
        .toList(growable: false);
  }
}

final class DashboardStatsDto {
  const DashboardStatsDto({
    required this.totalReceivables,
    required this.activeOrders,
    required this.totalPharmacies,
    required this.totalProducts,
    required this.dailySales,
    required this.monthlySales,
  });

  factory DashboardStatsDto.fromJson(Map<String, dynamic> json) {
    return DashboardStatsDto(
      totalReceivables: _double(json['totalReceivables']),
      activeOrders: _int(json['activeOrders']),
      totalPharmacies: _int(json['totalPharmacies']),
      totalProducts: _int(json['totalProducts']),
      dailySales: _double(json['dailySales']),
      monthlySales: _double(json['monthlySales']),
    );
  }

  final double totalReceivables;
  final int activeOrders;
  final int totalPharmacies;
  final int totalProducts;
  final double dailySales;
  final double monthlySales;
}

final class SalesDataPointDto {
  const SalesDataPointDto({required this.label, required this.value});

  factory SalesDataPointDto.fromJson(Map<String, dynamic> json) {
    return SalesDataPointDto(
      label: _string(json['label']),
      value: _double(json['value']),
    );
  }

  final String label;
  final double value;
}

final class ProductAlertDto {
  const ProductAlertDto({
    required this.id,
    required this.name,
    required this.quantity,
    required this.price,
    this.expiryDate,
  });

  factory ProductAlertDto.fromJson(Map<String, dynamic> json) {
    return ProductAlertDto(
      id: _int(json['id']),
      name: _string(json['name']),
      quantity: _int(json['quantity']),
      expiryDate: _nullableString(json['expiryDate']),
      price: _double(json['price'] ?? json['unitPrice']),
    );
  }

  final int id;
  final String name;
  final int quantity;
  final String? expiryDate;
  final double price;
}

final class PythonDashboardInsightsDto {
  const PythonDashboardInsightsDto({
    required this.generatedAt,
    required this.kpis,
    required this.recommendations,
  });

  factory PythonDashboardInsightsDto.fromJson(Map<String, dynamic> json) {
    return PythonDashboardInsightsDto(
      generatedAt: _string(json['generatedAt']),
      kpis: PythonKpisDto.fromJson(_map(json['kpis'])),
      recommendations: _list(
        json['recommendations'],
      ).map(PythonReorderRecommendationDto.fromJson).toList(growable: false),
    );
  }

  final String generatedAt;
  final PythonKpisDto kpis;
  final List<PythonReorderRecommendationDto> recommendations;
}

final class PythonKpisDto {
  const PythonKpisDto({
    required this.products,
    required this.pharmacies,
    required this.activeOrders,
    required this.dailySales,
    required this.monthlySales,
    required this.totalReceivables,
    required this.lowStock,
    required this.expiringSoon,
    required this.expired,
  });

  factory PythonKpisDto.fromJson(Map<String, dynamic> json) {
    return PythonKpisDto(
      products: _int(json['products']),
      pharmacies: _int(json['pharmacies']),
      activeOrders: _int(json['activeOrders']),
      dailySales: _double(json['dailySales']),
      monthlySales: _double(json['monthlySales']),
      totalReceivables: _double(json['totalReceivables']),
      lowStock: _int(json['lowStock']),
      expiringSoon: _int(json['expiringSoon']),
      expired: _int(json['expired']),
    );
  }

  final int products;
  final int pharmacies;
  final int activeOrders;
  final double dailySales;
  final double monthlySales;
  final double totalReceivables;
  final int lowStock;
  final int expiringSoon;
  final int expired;
}

final class PythonReorderRecommendationDto {
  const PythonReorderRecommendationDto({
    required this.productId,
    required this.name,
    required this.quantity,
    required this.salesLast30Days,
    required this.dailyVelocity,
    required this.suggestedQuantity,
    required this.urgencyScore,
  });

  factory PythonReorderRecommendationDto.fromJson(Map<String, dynamic> json) {
    return PythonReorderRecommendationDto(
      productId: _int(json['productId']),
      name: _string(json['name']),
      quantity: _int(json['quantity']),
      salesLast30Days: _int(json['salesLast30Days']),
      dailyVelocity: _double(json['dailyVelocity']),
      suggestedQuantity: _int(json['suggestedQuantity']),
      urgencyScore: _double(json['urgencyScore']),
    );
  }

  final int productId;
  final String name;
  final int quantity;
  final int salesLast30Days;
  final double dailyVelocity;
  final int suggestedQuantity;
  final double urgencyScore;
}

Map<String, dynamic> _map(Object? value) {
  if (value is Map<String, dynamic>) return value;
  return {};
}

List<Map<String, dynamic>> _list(Object? value) {
  if (value is! List) return const [];
  return value.whereType<Map<String, dynamic>>().toList(growable: false);
}

int _int(Object? value) {
  if (value is int) return value;
  if (value is num) return value.toInt();
  return int.tryParse('$value') ?? 0;
}

double _double(Object? value) {
  if (value is double) return value;
  if (value is num) return value.toDouble();
  return double.tryParse('$value') ?? 0;
}

String _string(Object? value) => value?.toString() ?? '';

String? _nullableString(Object? value) {
  final text = value?.toString();
  return text == null || text.isEmpty ? null : text;
}
