import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';

/// Enterprise color tokens used by both Flutter ThemeData and custom widgets.
///
/// Keep these values centralized. Feature widgets should read them through
/// `Theme.of(context).extension<AppThemeColors>()!` instead of hardcoding
/// colors, which keeps future brand refreshes cheap and safe.
@immutable
final class AppThemeColors extends ThemeExtension<AppThemeColors> {
  const AppThemeColors({
    required this.background,
    required this.surface,
    required this.surfaceElevated,
    required this.surfaceHover,
    required this.microBorder,
    required this.accentBorder,
    required this.textPrimary,
    required this.textSecondary,
    required this.textMuted,
    required this.cyan,
    required this.cyanBright,
    required this.emerald,
    required this.emeraldBright,
    required this.indigo,
    required this.warning,
    required this.danger,
  });

  final Color background;
  final Color surface;
  final Color surfaceElevated;
  final Color surfaceHover;
  final Color microBorder;
  final Color accentBorder;
  final Color textPrimary;
  final Color textSecondary;
  final Color textMuted;
  final Color cyan;
  final Color cyanBright;
  final Color emerald;
  final Color emeraldBright;
  final Color indigo;
  final Color warning;
  final Color danger;

  static const premiumDark = AppThemeColors(
    background: Color(0xFF0F172A), // Slate 900
    surface: Color(0xFF1E293B), // Slate 800
    surfaceElevated: Color(0xFF273449),
    surfaceHover: Color(0xFF334155), // Slate 700
    microBorder: Color(0x1AFFFFFF),
    accentBorder: Color(0x3306B6D4),
    textPrimary: Color(0xFFF8FAFC),
    textSecondary: Color(0xFFCBD5E1),
    textMuted: Color(0xFF94A3B8),
    cyan: Color(0xFF06B6D4),
    cyanBright: Color(0xFF22D3EE),
    emerald: Color(0xFF10B981),
    emeraldBright: Color(0xFF34D399),
    indigo: Color(0xFF6366F1),
    warning: Color(0xFFF59E0B),
    danger: Color(0xFFF43F5E),
  );

  @override
  AppThemeColors copyWith({
    Color? background,
    Color? surface,
    Color? surfaceElevated,
    Color? surfaceHover,
    Color? microBorder,
    Color? accentBorder,
    Color? textPrimary,
    Color? textSecondary,
    Color? textMuted,
    Color? cyan,
    Color? cyanBright,
    Color? emerald,
    Color? emeraldBright,
    Color? indigo,
    Color? warning,
    Color? danger,
  }) {
    return AppThemeColors(
      background: background ?? this.background,
      surface: surface ?? this.surface,
      surfaceElevated: surfaceElevated ?? this.surfaceElevated,
      surfaceHover: surfaceHover ?? this.surfaceHover,
      microBorder: microBorder ?? this.microBorder,
      accentBorder: accentBorder ?? this.accentBorder,
      textPrimary: textPrimary ?? this.textPrimary,
      textSecondary: textSecondary ?? this.textSecondary,
      textMuted: textMuted ?? this.textMuted,
      cyan: cyan ?? this.cyan,
      cyanBright: cyanBright ?? this.cyanBright,
      emerald: emerald ?? this.emerald,
      emeraldBright: emeraldBright ?? this.emeraldBright,
      indigo: indigo ?? this.indigo,
      warning: warning ?? this.warning,
      danger: danger ?? this.danger,
    );
  }

  @override
  AppThemeColors lerp(ThemeExtension<AppThemeColors>? other, double t) {
    if (other is! AppThemeColors) return this;

    return AppThemeColors(
      background: Color.lerp(background, other.background, t)!,
      surface: Color.lerp(surface, other.surface, t)!,
      surfaceElevated: Color.lerp(surfaceElevated, other.surfaceElevated, t)!,
      surfaceHover: Color.lerp(surfaceHover, other.surfaceHover, t)!,
      microBorder: Color.lerp(microBorder, other.microBorder, t)!,
      accentBorder: Color.lerp(accentBorder, other.accentBorder, t)!,
      textPrimary: Color.lerp(textPrimary, other.textPrimary, t)!,
      textSecondary: Color.lerp(textSecondary, other.textSecondary, t)!,
      textMuted: Color.lerp(textMuted, other.textMuted, t)!,
      cyan: Color.lerp(cyan, other.cyan, t)!,
      cyanBright: Color.lerp(cyanBright, other.cyanBright, t)!,
      emerald: Color.lerp(emerald, other.emerald, t)!,
      emeraldBright: Color.lerp(emeraldBright, other.emeraldBright, t)!,
      indigo: Color.lerp(indigo, other.indigo, t)!,
      warning: Color.lerp(warning, other.warning, t)!,
      danger: Color.lerp(danger, other.danger, t)!,
    );
  }
}

abstract final class AppSpacing {
  static const xs = 4.0;
  static const sm = 8.0;
  static const md = 12.0;
  static const lg = 16.0;
  static const xl = 24.0;
  static const xxl = 32.0;
}

abstract final class AppRadii {
  static const sm = Radius.circular(6);
  static const md = Radius.circular(8);
  static const lg = Radius.circular(10);

  static const card = BorderRadius.all(md);
  static const panel = BorderRadius.all(lg);
}

abstract final class AppBorders {
  static Border micro(AppThemeColors colors) {
    return Border.all(color: colors.microBorder);
  }

  static Border accent(AppThemeColors colors) {
    return Border.all(color: colors.accentBorder);
  }
}

/// Centralized Material theme for the Windows desktop admin application.
abstract final class AppTheme {
  static ThemeData get dark {
    const colors = AppThemeColors.premiumDark;

    final colorScheme = ColorScheme.dark(
      brightness: Brightness.dark,
      primary: colors.cyan,
      onPrimary: const Color(0xFF082F49),
      secondary: colors.emerald,
      onSecondary: const Color(0xFF052E2B),
      tertiary: colors.indigo,
      error: colors.danger,
      surface: colors.surface,
      onSurface: colors.textPrimary,
      outline: colors.microBorder,
      shadow: Colors.transparent,
    );

    final baseTextTheme = GoogleFonts.cairoTextTheme(
      ThemeData.dark(useMaterial3: true).textTheme,
    );

    final textTheme = baseTextTheme
        .apply(
          bodyColor: colors.textSecondary,
          displayColor: colors.textPrimary,
        )
        .copyWith(
          displayLarge: baseTextTheme.displayLarge?.copyWith(
            color: colors.textPrimary,
            fontWeight: FontWeight.w800,
            letterSpacing: 0,
          ),
          headlineLarge: baseTextTheme.headlineLarge?.copyWith(
            color: colors.textPrimary,
            fontWeight: FontWeight.w800,
            letterSpacing: 0,
          ),
          headlineMedium: baseTextTheme.headlineMedium?.copyWith(
            color: colors.textPrimary,
            fontWeight: FontWeight.w700,
            letterSpacing: 0,
          ),
          titleLarge: baseTextTheme.titleLarge?.copyWith(
            color: colors.textPrimary,
            fontWeight: FontWeight.w700,
            letterSpacing: 0,
          ),
          titleMedium: baseTextTheme.titleMedium?.copyWith(
            color: colors.textSecondary,
            fontWeight: FontWeight.w600,
            letterSpacing: 0,
          ),
          bodyLarge: baseTextTheme.bodyLarge?.copyWith(
            color: colors.textSecondary,
            height: 1.45,
            letterSpacing: 0,
          ),
          bodyMedium: baseTextTheme.bodyMedium?.copyWith(
            color: colors.textSecondary,
            height: 1.45,
            letterSpacing: 0,
          ),
          labelLarge: baseTextTheme.labelLarge?.copyWith(
            color: colors.textPrimary,
            fontWeight: FontWeight.w700,
            letterSpacing: 0,
          ),
          labelMedium: baseTextTheme.labelMedium?.copyWith(
            color: colors.textMuted,
            fontWeight: FontWeight.w600,
            letterSpacing: 0,
          ),
        );

    return ThemeData(
      useMaterial3: true,
      brightness: Brightness.dark,
      visualDensity: VisualDensity.compact,
      materialTapTargetSize: MaterialTapTargetSize.shrinkWrap,
      scaffoldBackgroundColor: colors.background,
      canvasColor: colors.background,
      colorScheme: colorScheme,
      textTheme: textTheme,
      fontFamily: GoogleFonts.cairo().fontFamily,
      splashFactory: InkRipple.splashFactory,
      extensions: const <ThemeExtension<dynamic>>[colors],
      appBarTheme: AppBarTheme(
        elevation: 0,
        scrolledUnderElevation: 0,
        centerTitle: false,
        backgroundColor: colors.background,
        foregroundColor: colors.textPrimary,
        titleTextStyle: textTheme.titleLarge,
        surfaceTintColor: Colors.transparent,
      ),
      cardTheme: CardThemeData(
        elevation: 0,
        margin: EdgeInsets.zero,
        color: colors.surface,
        surfaceTintColor: Colors.transparent,
        shape: RoundedRectangleBorder(
          borderRadius: AppRadii.card,
          side: BorderSide(color: colors.microBorder),
        ),
      ),
      dividerTheme: DividerThemeData(
        color: colors.microBorder,
        thickness: 1,
        space: 1,
      ),
      navigationRailTheme: NavigationRailThemeData(
        backgroundColor: colors.background,
        elevation: 0,
        indicatorColor: colors.surfaceHover,
        selectedIconTheme: IconThemeData(color: colors.cyanBright, size: 22),
        unselectedIconTheme: IconThemeData(color: colors.textMuted, size: 22),
        selectedLabelTextStyle: textTheme.labelLarge?.copyWith(
          color: colors.cyanBright,
          fontWeight: FontWeight.w800,
        ),
        unselectedLabelTextStyle: textTheme.labelMedium,
      ),
      elevatedButtonTheme: ElevatedButtonThemeData(
        style: ButtonStyle(
          elevation: const WidgetStatePropertyAll(0),
          shadowColor: const WidgetStatePropertyAll(Colors.transparent),
          backgroundColor: WidgetStateProperty.resolveWith((states) {
            if (states.contains(WidgetState.disabled)) {
              return colors.surfaceHover.withValues(alpha: 0.35);
            }
            if (states.contains(WidgetState.hovered)) return colors.cyanBright;
            return colors.cyan;
          }),
          foregroundColor: const WidgetStatePropertyAll(Color(0xFF082F49)),
          overlayColor: WidgetStatePropertyAll(
            colors.cyanBright.withValues(alpha: 0.12),
          ),
          padding: const WidgetStatePropertyAll(
            EdgeInsets.symmetric(
              horizontal: AppSpacing.xl,
              vertical: AppSpacing.md,
            ),
          ),
          minimumSize: const WidgetStatePropertyAll(Size(104, 40)),
          shape: WidgetStatePropertyAll(
            RoundedRectangleBorder(
              borderRadius: AppRadii.card,
              side: BorderSide(color: colors.accentBorder),
            ),
          ),
          textStyle: WidgetStatePropertyAll(textTheme.labelLarge),
        ),
      ),
      outlinedButtonTheme: OutlinedButtonThemeData(
        style: ButtonStyle(
          foregroundColor: WidgetStatePropertyAll(colors.textPrimary),
          overlayColor: WidgetStatePropertyAll(
            colors.surfaceHover.withValues(alpha: 0.35),
          ),
          side: WidgetStateProperty.resolveWith((states) {
            final color = states.contains(WidgetState.hovered)
                ? colors.accentBorder
                : colors.microBorder;
            return BorderSide(color: color);
          }),
          padding: const WidgetStatePropertyAll(
            EdgeInsets.symmetric(
              horizontal: AppSpacing.lg,
              vertical: AppSpacing.md,
            ),
          ),
          shape: WidgetStatePropertyAll(
            RoundedRectangleBorder(borderRadius: AppRadii.card),
          ),
          textStyle: WidgetStatePropertyAll(textTheme.labelLarge),
        ),
      ),
      textButtonTheme: TextButtonThemeData(
        style: ButtonStyle(
          foregroundColor: WidgetStatePropertyAll(colors.cyanBright),
          overlayColor: WidgetStatePropertyAll(
            colors.cyan.withValues(alpha: 0.08),
          ),
          shape: WidgetStatePropertyAll(
            RoundedRectangleBorder(borderRadius: AppRadii.card),
          ),
          textStyle: WidgetStatePropertyAll(textTheme.labelLarge),
        ),
      ),
      inputDecorationTheme: InputDecorationTheme(
        filled: true,
        fillColor: colors.surface,
        hintStyle: textTheme.bodyMedium?.copyWith(color: colors.textMuted),
        labelStyle: textTheme.labelMedium,
        contentPadding: const EdgeInsets.symmetric(
          horizontal: AppSpacing.lg,
          vertical: AppSpacing.md,
        ),
        border: _inputBorder(colors.microBorder),
        enabledBorder: _inputBorder(colors.microBorder),
        focusedBorder: _inputBorder(colors.accentBorder),
        errorBorder: _inputBorder(colors.danger.withValues(alpha: 0.5)),
        focusedErrorBorder: _inputBorder(colors.danger),
      ),
      dataTableTheme: DataTableThemeData(
        headingRowColor: WidgetStatePropertyAll(colors.surfaceElevated),
        dataRowColor: WidgetStateProperty.resolveWith((states) {
          if (states.contains(WidgetState.hovered)) return colors.surfaceHover;
          if (states.contains(WidgetState.selected)) {
            return colors.cyan.withValues(alpha: 0.12);
          }
          return colors.surface;
        }),
        dividerThickness: 1,
        headingTextStyle: textTheme.labelLarge?.copyWith(
          color: colors.textSecondary,
        ),
        dataTextStyle: textTheme.bodyMedium?.copyWith(
          color: colors.textPrimary,
        ),
        decoration: BoxDecoration(
          border: AppBorders.micro(colors),
          borderRadius: AppRadii.card,
        ),
      ),
      scrollbarTheme: ScrollbarThemeData(
        thumbColor: WidgetStateProperty.resolveWith((states) {
          if (states.contains(WidgetState.hovered)) {
            return colors.cyan.withValues(alpha: 0.55);
          }
          return colors.surfaceHover;
        }),
        trackColor: WidgetStatePropertyAll(
          colors.surface.withValues(alpha: 0.45),
        ),
        radius: AppRadii.sm,
        thickness: const WidgetStatePropertyAll(8),
        mainAxisMargin: AppSpacing.sm,
        crossAxisMargin: AppSpacing.sm,
      ),
      tooltipTheme: TooltipThemeData(
        decoration: BoxDecoration(
          color: colors.surfaceElevated,
          border: AppBorders.micro(colors),
          borderRadius: AppRadii.card,
        ),
        textStyle: textTheme.labelMedium?.copyWith(color: colors.textPrimary),
      ),
      popupMenuTheme: PopupMenuThemeData(
        elevation: 0,
        color: colors.surfaceElevated,
        surfaceTintColor: Colors.transparent,
        shape: RoundedRectangleBorder(
          borderRadius: AppRadii.card,
          side: BorderSide(color: colors.microBorder),
        ),
        textStyle: textTheme.bodyMedium,
      ),
      dialogTheme: DialogThemeData(
        elevation: 0,
        backgroundColor: colors.surface,
        surfaceTintColor: Colors.transparent,
        shape: RoundedRectangleBorder(
          borderRadius: AppRadii.panel,
          side: BorderSide(color: colors.microBorder),
        ),
        titleTextStyle: textTheme.titleLarge,
        contentTextStyle: textTheme.bodyMedium,
      ),
    );
  }

  static OutlineInputBorder _inputBorder(Color color) {
    return OutlineInputBorder(
      borderRadius: AppRadii.card,
      borderSide: BorderSide(color: color),
    );
  }
}
