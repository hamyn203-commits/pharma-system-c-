import 'package:al_neda_admin_flutter/core/theme/app_theme.dart';
import 'package:al_neda_admin_flutter/features/dashboard/data/dashboard_api.dart';
import 'package:al_neda_admin_flutter/features/dashboard/presentation/widgets/stat_card.dart';
import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:intl/intl.dart';

class StatsGrid extends StatelessWidget {
  const StatsGrid({super.key, this.stats});

  final DashboardStatsDto? stats;

  @override
  Widget build(BuildContext context) {
    final colors = Theme.of(context).extension<AppThemeColors>()!;
    final s = stats;
    final metrics = [
      StatCardData(
        title: 'إجمالي المستحقات',
        value: s != null ? _format(s.totalReceivables) : '1,284,500',
        suffix: 'ج.م',
        icon: Icons.account_balance_wallet_rounded,
        accentColor: colors.danger,
      ),
      StatCardData(
        title: 'الطلبات النشطة',
        value: s != null ? '${s.activeOrders}' : '42',
        suffix: 'طلب',
        icon: Icons.pending_actions_rounded,
        accentColor: colors.warning,
      ),
      StatCardData(
        title: 'مبيعات اليوم',
        value: s != null ? _format(s.dailySales) : '128,450',
        suffix: 'ج.م',
        icon: Icons.trending_up_rounded,
        accentColor: colors.cyan,
      ),
      StatCardData(
        title: 'المنتجات',
        value: s != null ? '${s.totalProducts}' : '3,842',
        suffix: 'صنف',
        icon: Icons.medication_rounded,
        accentColor: colors.emerald,
      ),
    ];

    return LayoutBuilder(
      builder: (context, constraints) {
        final width = constraints.maxWidth;
        final columns = width >= 1180
            ? 4
            : width >= 760
            ? 2
            : 1;

        return GridView.builder(
          shrinkWrap: true,
          physics: const NeverScrollableScrollPhysics(),
          itemCount: metrics.length,
          gridDelegate: SliverGridDelegateWithFixedCrossAxisCount(
            crossAxisCount: columns,
            crossAxisSpacing: AppSpacing.lg,
            mainAxisSpacing: AppSpacing.lg,
            mainAxisExtent: 172,
          ),
          itemBuilder: (context, index) {
            return StatCard(
              data: metrics[index],
              onTap: () {
                final routes = [
                  '/dashboard/pharmacies',
                  '/dashboard/orders',
                  '/dashboard/reports',
                  '/dashboard/products',
                ];
                if (index < routes.length) context.push(routes[index]);
              },
            );
          },
        );
      },
    );
  }

  String _format(double value) {
    return NumberFormat('#,##0').format(value);
  }
}
