import 'package:al_neda_admin_flutter/core/theme/app_theme.dart';
import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

class AdminSidebar extends StatelessWidget {
  const AdminSidebar({
    required this.activeRouteName,
    required this.compact,
    super.key,
  });

  final String activeRouteName;
  final bool compact;

  static const _items = [
    _SidebarItemData(
      routeName: 'dashboard',
      label: 'لوحة التحكم',
      icon: Icons.dashboard_rounded,
    ),
    _SidebarItemData(
      routeName: 'products',
      label: 'المنتجات',
      icon: Icons.medication_rounded,
    ),
    _SidebarItemData(
      routeName: 'pharmacies',
      label: 'الصيدليات',
      icon: Icons.local_pharmacy_rounded,
    ),
    _SidebarItemData(
      routeName: 'orders',
      label: 'الطلبات',
      icon: Icons.receipt_long_rounded,
    ),
    _SidebarItemData(
      routeName: 'settings',
      label: 'الإعدادات',
      icon: Icons.settings_rounded,
    ),
  ];

  @override
  Widget build(BuildContext context) {
    final colors = Theme.of(context).extension<AppThemeColors>()!;
    final textTheme = Theme.of(context).textTheme;

    return Container(
      margin: const EdgeInsets.fromLTRB(
        AppSpacing.xl,
        AppSpacing.xl,
        AppSpacing.xl,
        AppSpacing.xl,
      ),
      padding: EdgeInsets.all(compact ? AppSpacing.md : AppSpacing.lg),
      decoration: BoxDecoration(
        color: colors.surface,
        border: AppBorders.micro(colors),
        borderRadius: AppRadii.panel,
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          _SidebarBrand(compact: compact),
          const SizedBox(height: AppSpacing.xxl),
          Expanded(
            child: ListView.separated(
              itemCount: _items.length,
              separatorBuilder: (_, _) => const SizedBox(height: AppSpacing.sm),
              itemBuilder: (context, index) {
                final item = _items[index];
                return _SidebarNavigationItem(
                  item: item,
                  compact: compact,
                  active: item.routeName == activeRouteName,
                  onPressed: () {
                    context.go('/dashboard/${item.routeName}');
                  },
                );
              },
            ),
          ),
          if (!compact) ...[
            const SizedBox(height: AppSpacing.lg),
            Container(
              padding: const EdgeInsets.all(AppSpacing.lg),
              decoration: BoxDecoration(
                color: colors.surfaceElevated,
                border: AppBorders.micro(colors),
                borderRadius: AppRadii.card,
              ),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text('حالة النظام', style: textTheme.labelMedium),
                  const SizedBox(height: AppSpacing.sm),
                  Text(
                    'API جاهز للربط',
                    style: textTheme.titleMedium?.copyWith(
                      color: colors.emeraldBright,
                      fontWeight: FontWeight.w800,
                    ),
                  ),
                ],
              ),
            ),
          ],
        ],
      ),
    );
  }
}

class _SidebarBrand extends StatelessWidget {
  const _SidebarBrand({required this.compact});

  final bool compact;

  @override
  Widget build(BuildContext context) {
    final colors = Theme.of(context).extension<AppThemeColors>()!;
    final textTheme = Theme.of(context).textTheme;

    return Row(
      mainAxisAlignment: compact
          ? MainAxisAlignment.center
          : MainAxisAlignment.start,
      children: [
        Container(
          width: 44,
          height: 44,
          decoration: BoxDecoration(
            color: colors.cyan.withValues(alpha: 0.14),
            border: Border.all(color: colors.accentBorder),
            borderRadius: AppRadii.card,
          ),
          child: Icon(
            Icons.warehouse_rounded,
            color: colors.cyanBright,
            size: 24,
          ),
        ),
        if (!compact) ...[
          const SizedBox(width: AppSpacing.md),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  'مخزن الندى',
                  style: textTheme.titleLarge?.copyWith(
                    color: colors.textPrimary,
                    fontWeight: FontWeight.w900,
                  ),
                ),
                Text('لوحة الإدارة', style: textTheme.labelMedium),
              ],
            ),
          ),
        ],
      ],
    );
  }
}

class _SidebarNavigationItem extends StatefulWidget {
  const _SidebarNavigationItem({
    required this.item,
    required this.compact,
    required this.active,
    required this.onPressed,
  });

  final _SidebarItemData item;
  final bool compact;
  final bool active;
  final VoidCallback onPressed;

  @override
  State<_SidebarNavigationItem> createState() => _SidebarNavigationItemState();
}

class _SidebarNavigationItemState extends State<_SidebarNavigationItem> {
  bool _hovered = false;

  @override
  Widget build(BuildContext context) {
    final colors = Theme.of(context).extension<AppThemeColors>()!;
    final textTheme = Theme.of(context).textTheme;
    final selected = widget.active;
    final backgroundColor = selected || _hovered
        ? colors.surfaceHover
        : Colors.transparent;
    final foregroundColor = selected ? colors.cyanBright : colors.textSecondary;

    return MouseRegion(
      onEnter: (_) => setState(() => _hovered = true),
      onExit: (_) => setState(() => _hovered = false),
      child: AnimatedContainer(
        duration: const Duration(milliseconds: 140),
        curve: Curves.easeOutCubic,
        decoration: BoxDecoration(
          color: backgroundColor,
          border: Border.all(
            color: selected ? colors.accentBorder : colors.microBorder,
          ),
          borderRadius: AppRadii.card,
        ),
        child: Stack(
          alignment: Alignment.centerLeft,
          children: [
            AnimatedContainer(
              width: selected ? 3 : 0,
              height: 28,
              duration: const Duration(milliseconds: 140),
              decoration: BoxDecoration(
                color: colors.cyanBright,
                borderRadius: BorderRadius.circular(99),
              ),
            ),
            Material(
              color: Colors.transparent,
              child: InkWell(
                borderRadius: AppRadii.card,
                onTap: widget.onPressed,
                child: Padding(
                  padding: EdgeInsets.symmetric(
                    horizontal: widget.compact ? 0 : AppSpacing.md,
                    vertical: AppSpacing.md,
                  ),
                  child: Row(
                    mainAxisAlignment: widget.compact
                        ? MainAxisAlignment.center
                        : MainAxisAlignment.start,
                    children: [
                      Icon(widget.item.icon, color: foregroundColor, size: 22),
                      if (!widget.compact) ...[
                        const SizedBox(width: AppSpacing.md),
                        Expanded(
                          child: Text(
                            widget.item.label,
                            style: textTheme.labelLarge?.copyWith(
                              color: foregroundColor,
                              fontWeight: selected
                                  ? FontWeight.w900
                                  : FontWeight.w700,
                            ),
                          ),
                        ),
                      ],
                    ],
                  ),
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _SidebarItemData {
  const _SidebarItemData({
    required this.routeName,
    required this.label,
    required this.icon,
  });

  final String routeName;
  final String label;
  final IconData icon;
}
