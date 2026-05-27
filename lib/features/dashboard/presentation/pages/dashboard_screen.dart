import 'package:al_neda_admin_flutter/core/theme/app_theme.dart';
import 'package:al_neda_admin_flutter/features/dashboard/presentation/cubit/dashboard_cubit.dart';
import 'package:al_neda_admin_flutter/features/dashboard/presentation/widgets/admin_sidebar.dart';
import 'package:al_neda_admin_flutter/features/dashboard/presentation/widgets/inventory_alerts.dart';
import 'package:al_neda_admin_flutter/features/dashboard/presentation/widgets/sales_trend_chart.dart';
import 'package:al_neda_admin_flutter/features/dashboard/presentation/widgets/stats_grid.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';
import 'package:intl/intl.dart' as intl;

/// Desktop-first admin dashboard shell.
///
/// The sidebar is intentionally placed as the last child in an LTR row so it
/// renders on the physical right side while the rest of the app remains RTL.
class DashboardScreen extends StatelessWidget {
  const DashboardScreen({super.key});

  @override
  Widget build(BuildContext context) {
    final colors = Theme.of(context).extension<AppThemeColors>()!;

    return BlocProvider(
      create: (_) => DashboardCubit()..load(),
      child: Scaffold(
        backgroundColor: colors.background,
        body: SafeArea(
          child: LayoutBuilder(
            builder: (context, constraints) {
              final isCompact = constraints.maxWidth < 1240;
              final sidebarWidth = isCompact ? 92.0 : 292.0;

              return Row(
                textDirection: TextDirection.ltr,
                children: [
                  Expanded(child: _DashboardContent(isCompact: isCompact)),
                  SizedBox(
                    width: sidebarWidth,
                    child: AdminSidebar(
                      activeRouteName: 'dashboard',
                      compact: isCompact,
                    ),
                  ),
                ],
              );
            },
          ),
        ),
      ),
    );
  }
}

class _DashboardContent extends StatefulWidget {
  const _DashboardContent({required this.isCompact});

  final bool isCompact;

  @override
  State<_DashboardContent> createState() => _DashboardContentState();
}

class _DashboardContentState extends State<_DashboardContent> {
  final ScrollController _scrollController = ScrollController();

  @override
  void dispose() {
    _scrollController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final colors = Theme.of(context).extension<AppThemeColors>()!;

    return BlocConsumer<DashboardCubit, DashboardState>(
      listener: (context, state) {
        if (state.error != null && state.error!.isNotEmpty) {
          ScaffoldMessenger.of(context).showSnackBar(
            SnackBar(content: Text('خطأ في تحميل البيانات: ${state.error}')),
          );
        }
      },
      builder: (context, state) {
        return Stack(
          children: [
            Scrollbar(
              controller: _scrollController,
              thumbVisibility: true,
              child: CustomScrollView(
                controller: _scrollController,
                primary: false,
                slivers: [
                  SliverPadding(
                    padding: EdgeInsets.fromLTRB(
                      widget.isCompact ? AppSpacing.lg : AppSpacing.xxl,
                      AppSpacing.xl,
                      widget.isCompact ? AppSpacing.lg : AppSpacing.xxl,
                      AppSpacing.xxl,
                    ),
                    sliver: SliverList.list(
                      children: [
                        _DashboardHeader(),
                        const SizedBox(height: AppSpacing.xl),
                        StatsGrid(stats: state.stats),
                        const SizedBox(height: AppSpacing.xl),
                        LayoutBuilder(
                          builder: (context, constraints) {
                            final stackSections = constraints.maxWidth < 1120;

                            if (stackSections) {
                              return Column(
                                children: [
                                  SalesTrendChart(salesData: state.salesData),
                                  const SizedBox(height: AppSpacing.xl),
                                  InventoryAlerts(
                                    lowStockAlerts: state.lowStockAlerts,
                                    expiringAlerts: state.expiringAlerts,
                                  ),
                                ],
                              );
                            }

                            return Row(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                Expanded(
                                  flex: 7,
                                  child: SalesTrendChart(
                                    salesData: state.salesData,
                                  ),
                                ),
                                const SizedBox(width: AppSpacing.xl),
                                Expanded(
                                  flex: 4,
                                  child: InventoryAlerts(
                                    lowStockAlerts: state.lowStockAlerts,
                                    expiringAlerts: state.expiringAlerts,
                                  ),
                                ),
                              ],
                            );
                          },
                        ),
                        const SizedBox(height: AppSpacing.xl),
                        _OperationsStrip(colors: colors),
                      ],
                    ),
                  ),
                ],
              ),
            ),
            if (state.loading)
              Positioned.fill(
                child: Container(
                  color: colors.background.withValues(alpha: 0.6),
                  child: const Center(
                    child: CircularProgressIndicator(),
                  ),
                ),
              ),
          ],
        );
      },
    );
  }
}

class _DashboardHeader extends StatelessWidget {
  @override
  Widget build(BuildContext context) {
    final colors = Theme.of(context).extension<AppThemeColors>()!;
    final textTheme = Theme.of(context).textTheme;
    final dateLabel = intl.DateFormat(
      'EEEE، d MMMM yyyy',
      'ar_EG',
    ).format(DateTime.now());

    return Container(
      padding: const EdgeInsets.all(AppSpacing.xl),
      decoration: BoxDecoration(
        color: colors.surface,
        border: AppBorders.accent(colors),
        borderRadius: AppRadii.panel,
      ),
      child: LayoutBuilder(
        builder: (context, constraints) {
          final stack = constraints.maxWidth < 860;

          final title = Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                'لوحة التحكم',
                style: textTheme.headlineLarge?.copyWith(
                  fontWeight: FontWeight.w900,
                ),
              ),
              const SizedBox(height: AppSpacing.sm),
              Text(
                dateLabel,
                style: textTheme.bodyMedium?.copyWith(color: colors.textMuted),
              ),
            ],
          );

          final actions = Wrap(
            spacing: AppSpacing.md,
            runSpacing: AppSpacing.md,
            alignment: WrapAlignment.end,
            children: [
              _HeaderAction(
                label: 'طلب جديد',
                icon: Icons.add_shopping_cart_rounded,
                color: colors.cyan,
              ),
              _HeaderAction(
                label: 'تقرير سريع',
                icon: Icons.summarize_rounded,
                color: colors.emerald,
              ),
              _StatusPill(
                label: 'النظام متصل',
                icon: Icons.cloud_done_rounded,
                color: colors.emerald,
              ),
            ],
          );

          final body = [
            Expanded(child: title),
            const SizedBox(width: AppSpacing.xl),
            Flexible(child: actions),
          ];

          if (stack) {
            return Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                title,
                const SizedBox(height: AppSpacing.lg),
                actions,
              ],
            );
          }

          return Row(children: body);
        },
      ),
    );
  }
}

class _HeaderAction extends StatelessWidget {
  const _HeaderAction({
    required this.label,
    required this.icon,
    required this.color,
  });

  final String label;
  final IconData icon;
  final Color color;

  @override
  Widget build(BuildContext context) {
    final colors = Theme.of(context).extension<AppThemeColors>()!;
    final textTheme = Theme.of(context).textTheme;

    return OutlinedButton.icon(
      onPressed: () {
        if (label.contains('طلب جديد')) {
          context.push('/dashboard/orders');
        } else if (label.contains('تقرير')) {
          context.push('/dashboard/reports');
        }
      },
      icon: Icon(icon, color: color, size: 18),
      label: Text(
        label,
        style: textTheme.labelLarge?.copyWith(color: colors.textPrimary),
      ),
    );
  }
}

class _StatusPill extends StatelessWidget {
  const _StatusPill({
    required this.label,
    required this.icon,
    required this.color,
  });

  final String label;
  final IconData icon;
  final Color color;

  @override
  Widget build(BuildContext context) {
    final colors = Theme.of(context).extension<AppThemeColors>()!;
    final textTheme = Theme.of(context).textTheme;

    return Container(
      padding: const EdgeInsets.symmetric(
        horizontal: AppSpacing.md,
        vertical: AppSpacing.sm,
      ),
      decoration: BoxDecoration(
        color: color.withValues(alpha: 0.12),
        border: Border.all(color: color.withValues(alpha: 0.22)),
        borderRadius: AppRadii.card,
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(icon, color: color, size: 18),
          const SizedBox(width: AppSpacing.sm),
          Text(
            label,
            style: textTheme.labelLarge?.copyWith(color: colors.textPrimary),
          ),
        ],
      ),
    );
  }
}

class _OperationsStrip extends StatelessWidget {
  const _OperationsStrip({required this.colors});

  final AppThemeColors colors;

  @override
  Widget build(BuildContext context) {
    final textTheme = Theme.of(context).textTheme;

    return Container(
      padding: const EdgeInsets.all(AppSpacing.xl),
      decoration: BoxDecoration(
        color: colors.surface,
        border: AppBorders.micro(colors),
        borderRadius: AppRadii.panel,
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Icon(Icons.auto_awesome_rounded, color: colors.cyan, size: 22),
              const SizedBox(width: AppSpacing.md),
              Text(
                'توصيات التشغيل',
                style: textTheme.titleMedium?.copyWith(
                  color: colors.textPrimary,
                  fontWeight: FontWeight.w900,
                ),
              ),
            ],
          ),
          const SizedBox(height: AppSpacing.lg),
          LayoutBuilder(
            builder: (context, constraints) {
              final stack = constraints.maxWidth < 900;
              final children = [
                _RecommendationTile(
                  icon: Icons.inventory_2_rounded,
                  title: 'راجع الأصناف منخفضة المخزون',
                  value: '18 صنف',
                  color: colors.warning,
                  onTap: () => context.push('/dashboard/products'),
                ),
                _RecommendationTile(
                  icon: Icons.event_busy_rounded,
                  title: 'تابع تواريخ الصلاحية القريبة',
                  value: '7 تنبيهات',
                  color: colors.danger,
                  onTap: () => context.push('/dashboard/products'),
                ),
                _RecommendationTile(
                  icon: Icons.trending_up_rounded,
                  title: 'مبيعات اليوم مستقرة',
                  value: '+12%',
                  color: colors.emerald,
                  onTap: () => context.push('/dashboard/reports'),
                ),
              ];

              if (stack) {
                return Column(
                  children: children
                      .map(
                        (child) => Padding(
                          padding: const EdgeInsets.only(bottom: AppSpacing.md),
                          child: child,
                        ),
                      )
                      .toList(growable: false),
                );
              }

              return Row(
                children: children
                    .map((child) => Expanded(child: child))
                    .expand(
                      (child) => [child, const SizedBox(width: AppSpacing.md)],
                    )
                    .take(children.length * 2 - 1)
                    .toList(growable: false),
              );
            },
          ),
        ],
      ),
    );
  }
}

class _RecommendationTile extends StatefulWidget {
  const _RecommendationTile({
    required this.icon,
    required this.title,
    required this.value,
    required this.color,
    this.onTap,
  });

  final IconData icon;
  final String title;
  final String value;
  final Color color;
  final VoidCallback? onTap;

  @override
  State<_RecommendationTile> createState() => _RecommendationTileState();
}

class _RecommendationTileState extends State<_RecommendationTile> {
  bool _hovered = false;

  @override
  Widget build(BuildContext context) {
    final colors = Theme.of(context).extension<AppThemeColors>()!;
    final textTheme = Theme.of(context).textTheme;

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
                  ? widget.color.withValues(alpha: 0.28)
                  : colors.microBorder,
            ),
            borderRadius: AppRadii.card,
          ),
          child: Row(
            children: [
              Container(
                width: 42,
                height: 42,
                decoration: BoxDecoration(
                  color: widget.color.withValues(alpha: 0.12),
                  border: Border.all(
                    color: widget.color.withValues(alpha: 0.22),
                  ),
                  borderRadius: AppRadii.card,
                ),
                child: Icon(widget.icon, color: widget.color, size: 22),
              ),
              const SizedBox(width: AppSpacing.md),
              Expanded(
                child: Text(
                  widget.title,
                  style: textTheme.bodyMedium?.copyWith(
                    color: colors.textSecondary,
                    fontWeight: FontWeight.w700,
                  ),
                ),
              ),
              Text(
                widget.value,
                style: textTheme.titleMedium?.copyWith(
                  color: widget.color,
                  fontWeight: FontWeight.w900,
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
