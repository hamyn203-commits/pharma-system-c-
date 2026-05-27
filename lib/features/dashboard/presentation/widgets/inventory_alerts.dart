import 'package:al_neda_admin_flutter/core/theme/app_theme.dart';
import 'package:al_neda_admin_flutter/features/dashboard/data/dashboard_api.dart';
import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

class InventoryAlerts extends StatelessWidget {
  const InventoryAlerts({super.key, this.lowStockAlerts, this.expiringAlerts});

  final List<ProductAlertDto>? lowStockAlerts;
  final List<ProductAlertDto>? expiringAlerts;

  @override
  Widget build(BuildContext context) {
    final colors = Theme.of(context).extension<AppThemeColors>()!;
    final alerts = <_InventoryAlertData>[
      if (lowStockAlerts != null)
        for (final a in lowStockAlerts!)
          _InventoryAlertData(
            productName: a.name,
            detail: 'المتبقي ${a.quantity} وحدة فقط',
            severity: _InventoryAlertSeverity.warning,
            color: colors.warning,
          ),
      if (expiringAlerts != null)
        for (final a in expiringAlerts!)
          _InventoryAlertData(
            productName: a.name,
            detail: a.expiryDate != null
                ? 'ينتهي ${a.expiryDate}'
                : 'الكمية تحت حد الأمان',
            severity: _InventoryAlertSeverity.danger,
            color: colors.danger,
          ),
      if (lowStockAlerts == null && expiringAlerts == null) ...[
        _InventoryAlertData(
          productName: 'باراسيتامول 500 مجم',
          detail: 'المتبقي 8 علب فقط',
          severity: _InventoryAlertSeverity.warning,
          color: colors.warning,
        ),
        _InventoryAlertData(
          productName: 'أموكسيسيلين شراب',
          detail: 'ينتهي خلال 21 يوم',
          severity: _InventoryAlertSeverity.danger,
          color: colors.danger,
        ),
        _InventoryAlertData(
          productName: 'فيتامين د نقط',
          detail: 'المتبقي 11 وحدة',
          severity: _InventoryAlertSeverity.warning,
          color: colors.warning,
        ),
        _InventoryAlertData(
          productName: 'مطهر جروح 100 مل',
          detail: 'الكمية تحت حد الأمان',
          severity: _InventoryAlertSeverity.danger,
          color: colors.danger,
        ),
      ],
    ];

    return Container(
      height: 430,
      padding: const EdgeInsets.all(AppSpacing.xl),
      decoration: BoxDecoration(
        color: colors.surface,
        border: AppBorders.micro(colors),
        borderRadius: AppRadii.panel,
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          _InventoryAlertsHeader(count: alerts.length),
          const SizedBox(height: AppSpacing.xl),
          Expanded(
            child: ListView.separated(
              physics: const NeverScrollableScrollPhysics(),
              itemCount: alerts.length,
              separatorBuilder: (_, _) => const SizedBox(height: AppSpacing.md),
              itemBuilder: (context, index) {
                final alert = alerts[index];
                return _InventoryAlertTile(
                  alert: alert,
                  onTap: () {
                    context.push('/dashboard/products');
                  },
                );
              },
            ),
          ),
        ],
      ),
    );
  }
}

class _InventoryAlertsHeader extends StatelessWidget {
  const _InventoryAlertsHeader({required this.count});

  final int count;

  @override
  Widget build(BuildContext context) {
    final colors = Theme.of(context).extension<AppThemeColors>()!;
    final textTheme = Theme.of(context).textTheme;

    return Row(
      children: [
        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text('تنبيهات المخزون', style: textTheme.titleLarge),
              const SizedBox(height: AppSpacing.xs),
              Text(
                'عناصر تحتاج متابعة من فريق المخزن.',
                style: textTheme.bodyMedium?.copyWith(color: colors.textMuted),
              ),
            ],
          ),
        ),
        Container(
          padding: const EdgeInsets.symmetric(
            horizontal: AppSpacing.md,
            vertical: AppSpacing.sm,
          ),
          decoration: BoxDecoration(
            color: colors.warning.withValues(alpha: 0.12),
            border: Border.all(color: colors.warning.withValues(alpha: 0.25)),
            borderRadius: AppRadii.card,
          ),
          child: Text(
            '$count تنبيهات',
            style: textTheme.labelLarge?.copyWith(color: colors.warning),
          ),
        ),
      ],
    );
  }
}

class _InventoryAlertTile extends StatefulWidget {
  const _InventoryAlertTile({required this.alert, this.onTap});

  final _InventoryAlertData alert;
  final VoidCallback? onTap;

  @override
  State<_InventoryAlertTile> createState() => _InventoryAlertTileState();
}

class _InventoryAlertTileState extends State<_InventoryAlertTile> {
  bool _hovered = false;

  @override
  Widget build(BuildContext context) {
    final colors = Theme.of(context).extension<AppThemeColors>()!;
    final textTheme = Theme.of(context).textTheme;
    final alert = widget.alert;

    return MouseRegion(
      onEnter: (_) => setState(() => _hovered = true),
      onExit: (_) => setState(() => _hovered = false),
      child: GestureDetector(
        onTap: widget.onTap,
        child: AnimatedContainer(
          duration: const Duration(milliseconds: 140),
          padding: const EdgeInsets.all(AppSpacing.lg),
          decoration: BoxDecoration(
            color: _hovered ? colors.surfaceHover : colors.surfaceElevated,
            border: Border.all(
              color: _hovered
                  ? alert.color.withValues(alpha: 0.28)
                  : colors.microBorder,
            ),
            borderRadius: AppRadii.card,
          ),
          child: Row(
            children: [
              Container(
                width: 40,
                height: 40,
                decoration: BoxDecoration(
                  color: alert.color.withValues(alpha: 0.14),
                  border: Border.all(
                    color: alert.color.withValues(alpha: 0.24),
                  ),
                  borderRadius: AppRadii.card,
                ),
                child: Icon(alert.severity.icon, color: alert.color, size: 21),
              ),
              const SizedBox(width: AppSpacing.md),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      alert.productName,
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                      style: textTheme.labelLarge?.copyWith(
                        color: colors.textPrimary,
                      ),
                    ),
                    const SizedBox(height: AppSpacing.xs),
                    Text(
                      alert.detail,
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                      style: textTheme.labelMedium?.copyWith(
                        color: alert.color,
                      ),
                    ),
                  ],
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

class _InventoryAlertData {
  const _InventoryAlertData({
    required this.productName,
    required this.detail,
    required this.severity,
    required this.color,
  });

  final String productName;
  final String detail;
  final _InventoryAlertSeverity severity;
  final Color color;
}

enum _InventoryAlertSeverity {
  warning(Icons.inventory_2_rounded),
  danger(Icons.error_rounded);

  const _InventoryAlertSeverity(this.icon);

  final IconData icon;
}
