import 'package:al_neda_admin_flutter/core/network/api_client.dart';
import 'package:al_neda_admin_flutter/features/dashboard/data/dashboard_api.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

final class DashboardState {
  const DashboardState({
    this.loading = false,
    this.error,
    this.stats,
    this.salesData,
    this.lowStockAlerts,
    this.expiringAlerts,
  });

  final bool loading;
  final String? error;
  final DashboardStatsDto? stats;
  final List<SalesDataPointDto>? salesData;
  final List<ProductAlertDto>? lowStockAlerts;
  final List<ProductAlertDto>? expiringAlerts;

  DashboardState copyWith({
    bool? loading,
    String? error,
    DashboardStatsDto? stats,
    List<SalesDataPointDto>? salesData,
    List<ProductAlertDto>? lowStockAlerts,
    List<ProductAlertDto>? expiringAlerts,
    bool clearError = false,
  }) {
    return DashboardState(
      loading: loading ?? this.loading,
      error: clearError ? null : (error ?? this.error),
      stats: stats ?? this.stats,
      salesData: salesData ?? this.salesData,
      lowStockAlerts: lowStockAlerts ?? this.lowStockAlerts,
      expiringAlerts: expiringAlerts ?? this.expiringAlerts,
    );
  }

  static final initial = DashboardState(
    loading: false,
    stats: null,
    salesData: null,
    lowStockAlerts: null,
    expiringAlerts: null,
  );
}

final class DashboardCubit extends Cubit<DashboardState> {
  DashboardCubit({DashboardApi? api})
    : _api = api ?? DashboardApi(ApiClient.local()),
      super(DashboardState.initial);

  final DashboardApi _api;

  Future<void> load() async {
    emit(state.copyWith(loading: true, clearError: true));
    try {
      final results = await Future.wait([
        _api.getStats(),
        _api.getLast30DaysSales(),
        _api.getLowStockAlerts(),
        _api.getExpiringAlerts(),
      ]);
      emit(
        state.copyWith(
          loading: false,
          stats: results[0] as DashboardStatsDto,
          salesData: results[1] as List<SalesDataPointDto>,
          lowStockAlerts: results[2] as List<ProductAlertDto>,
          expiringAlerts: results[3] as List<ProductAlertDto>,
        ),
      );
    } catch (e) {
      emit(state.copyWith(loading: false, error: e.toString()));
    }
  }
}
