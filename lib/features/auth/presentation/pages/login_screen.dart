import 'package:al_neda_admin_flutter/core/network/api_client.dart';
import 'package:al_neda_admin_flutter/core/theme/app_theme.dart';
import 'package:al_neda_admin_flutter/features/auth/data/auth_api.dart';
import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

enum _AuthMode { login, register }

class LoginScreen extends StatefulWidget {
  const LoginScreen({super.key, AuthApi? authApi}) : _providedAuthApi = authApi;

  final AuthApi? _providedAuthApi;

  @override
  State<LoginScreen> createState() => _LoginScreenState();
}

class _LoginScreenState extends State<LoginScreen> {
  late final AuthApi _authApi =
      widget._providedAuthApi ?? AuthApi(ApiClient.local());

  final _loginFormKey = GlobalKey<FormState>();
  final _registerFormKey = GlobalKey<FormState>();
  final _usernameController = TextEditingController();
  final _passwordController = TextEditingController();
  final _pharmacyNameController = TextEditingController();
  final _registerUsernameController = TextEditingController();
  final _registerPasswordController = TextEditingController();
  final _phoneController = TextEditingController();
  final _addressController = TextEditingController();

  _AuthMode _mode = _AuthMode.login;
  bool _obscurePassword = true;
  bool _loading = false;
  String? _error;
  String? _successMessage;

  @override
  void dispose() {
    _usernameController.dispose();
    _passwordController.dispose();
    _pharmacyNameController.dispose();
    _registerUsernameController.dispose();
    _registerPasswordController.dispose();
    _phoneController.dispose();
    _addressController.dispose();
    super.dispose();
  }

  Future<void> _submitLogin() async {
    if (!_loginFormKey.currentState!.validate()) return;
    setState(() {
      _loading = true;
      _error = null;
      _successMessage = null;
    });

    try {
      await _authApi.login(
        username: _usernameController.text.trim(),
        password: _passwordController.text,
      );
      if (mounted) context.go('/dashboard');
    } catch (error) {
      setState(() => _error = _friendlyError(error));
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  Future<void> _submitRegister() async {
    if (!_registerFormKey.currentState!.validate()) return;
    setState(() {
      _loading = true;
      _error = null;
      _successMessage = null;
    });

    try {
      final result = await _authApi.registerPharmacy(
        pharmacyName: _pharmacyNameController.text.trim(),
        username: _registerUsernameController.text.trim(),
        password: _registerPasswordController.text,
        phone: _phoneController.text.trim(),
        address: _addressController.text.trim(),
      );
      setState(() {
        _mode = _AuthMode.login;
        _usernameController.text = result.username;
        _successMessage = result.message.isEmpty
            ? 'تم إنشاء الحساب وهو في انتظار الموافقة'
            : result.message;
      });
    } catch (error) {
      setState(() => _error = _friendlyError(error));
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  String _friendlyError(Object error) {
    final text = error.toString();
    if (text.contains('401')) return 'اسم المستخدم أو كلمة المرور غير صحيحة';
    if (text.contains('403')) return 'الحساب غير نشط أو في انتظار الموافقة';
    if (text.contains('409')) return 'البيانات مسجلة مسبقا';
    if (text.contains('SocketException') || text.contains('Connection')) {
      return 'تعذر الاتصال بالخادم';
    }
    return 'تعذر تنفيذ العملية الآن';
  }

  @override
  Widget build(BuildContext context) {
    final colors = Theme.of(context).extension<AppThemeColors>()!;

    return Scaffold(
      backgroundColor: colors.background,
      body: SafeArea(
        child: LayoutBuilder(
          builder: (context, constraints) {
            final compact = constraints.maxWidth < 920;

            return Center(
              child: SingleChildScrollView(
                padding: const EdgeInsets.all(AppSpacing.xxl),
                child: ConstrainedBox(
                  constraints: const BoxConstraints(maxWidth: 1180),
                  child: compact
                      ? Column(
                          children: [
                            const _BrandPanel(compact: true),
                            const SizedBox(height: AppSpacing.xl),
                            _AuthPanel(
                              mode: _mode,
                              loading: _loading,
                              error: _error,
                              successMessage: _successMessage,
                              obscurePassword: _obscurePassword,
                              onModeChanged: _setMode,
                              onTogglePassword: () => setState(
                                () => _obscurePassword = !_obscurePassword,
                              ),
                              loginFormKey: _loginFormKey,
                              registerFormKey: _registerFormKey,
                              usernameController: _usernameController,
                              passwordController: _passwordController,
                              pharmacyNameController: _pharmacyNameController,
                              registerUsernameController:
                                  _registerUsernameController,
                              registerPasswordController:
                                  _registerPasswordController,
                              phoneController: _phoneController,
                              addressController: _addressController,
                              onLogin: _submitLogin,
                              onRegister: _submitRegister,
                            ),
                          ],
                        )
                      : Row(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            const Expanded(child: _BrandPanel(compact: false)),
                            const SizedBox(width: AppSpacing.xl),
                            SizedBox(
                              width: 460,
                              child: _AuthPanel(
                                mode: _mode,
                                loading: _loading,
                                error: _error,
                                successMessage: _successMessage,
                                obscurePassword: _obscurePassword,
                                onModeChanged: _setMode,
                                onTogglePassword: () => setState(
                                  () => _obscurePassword = !_obscurePassword,
                                ),
                                loginFormKey: _loginFormKey,
                                registerFormKey: _registerFormKey,
                                usernameController: _usernameController,
                                passwordController: _passwordController,
                                pharmacyNameController: _pharmacyNameController,
                                registerUsernameController:
                                    _registerUsernameController,
                                registerPasswordController:
                                    _registerPasswordController,
                                phoneController: _phoneController,
                                addressController: _addressController,
                                onLogin: _submitLogin,
                                onRegister: _submitRegister,
                              ),
                            ),
                          ],
                        ),
                ),
              ),
            );
          },
        ),
      ),
    );
  }

  void _setMode(_AuthMode mode) {
    setState(() {
      _mode = mode;
      _error = null;
      _successMessage = null;
    });
  }
}

class _BrandPanel extends StatelessWidget {
  const _BrandPanel({required this.compact});

  final bool compact;

  @override
  Widget build(BuildContext context) {
    final colors = Theme.of(context).extension<AppThemeColors>()!;
    final textTheme = Theme.of(context).textTheme;

    return Container(
      constraints: BoxConstraints(minHeight: compact ? 260 : 620),
      padding: EdgeInsets.all(compact ? AppSpacing.xl : 42),
      decoration: BoxDecoration(
        color: colors.surface,
        border: AppBorders.accent(colors),
        borderRadius: AppRadii.panel,
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        children: [
          Row(
            children: [
              Container(
                width: 54,
                height: 54,
                decoration: BoxDecoration(
                  color: colors.cyan.withValues(alpha: 0.14),
                  border: Border.all(color: colors.accentBorder),
                  borderRadius: AppRadii.card,
                ),
                child: Icon(
                  Icons.warehouse_rounded,
                  color: colors.cyanBright,
                  size: 28,
                ),
              ),
              const SizedBox(width: AppSpacing.md),
              Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    'مخزن الندى',
                    style: textTheme.titleLarge?.copyWith(
                      color: colors.textPrimary,
                      fontWeight: FontWeight.w900,
                    ),
                  ),
                  Text(
                    'لوحة إدارة المخزن',
                    style: textTheme.labelMedium?.copyWith(
                      color: colors.textMuted,
                    ),
                  ),
                ],
              ),
            ],
          ),
          Padding(
            padding: EdgeInsets.only(top: compact ? AppSpacing.xl : 0),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  'إدارة الطلبات والمخزون في شاشة واحدة واضحة',
                  style:
                      (compact
                              ? textTheme.headlineMedium
                              : textTheme.displaySmall)
                          ?.copyWith(
                            color: colors.textPrimary,
                            fontWeight: FontWeight.w900,
                            letterSpacing: 0,
                            height: 1.25,
                          ),
                ),
                const SizedBox(height: AppSpacing.lg),
                Wrap(
                  spacing: AppSpacing.md,
                  runSpacing: AppSpacing.md,
                  children: [
                    _SignalChip(
                      icon: Icons.verified_user_rounded,
                      label: 'صلاحيات آمنة',
                      color: colors.emerald,
                    ),
                    _SignalChip(
                      icon: Icons.sync_rounded,
                      label: 'مزامنة فورية',
                      color: colors.cyan,
                    ),
                    _SignalChip(
                      icon: Icons.insights_rounded,
                      label: 'تحليلات ذكية',
                      color: colors.indigo,
                    ),
                  ],
                ),
              ],
            ),
          ),
          if (!compact)
            Row(
              children: [
                _MiniMetric(
                  label: 'طلبات نشطة',
                  value: '42',
                  color: colors.warning,
                ),
                const SizedBox(width: AppSpacing.md),
                _MiniMetric(
                  label: 'متابعة مخزون',
                  value: '18',
                  color: colors.emerald,
                ),
                const SizedBox(width: AppSpacing.md),
                _MiniMetric(label: 'تنبيهات', value: '7', color: colors.danger),
              ],
            ),
        ],
      ),
    );
  }
}

class _AuthPanel extends StatelessWidget {
  const _AuthPanel({
    required this.mode,
    required this.loading,
    required this.error,
    required this.successMessage,
    required this.obscurePassword,
    required this.onModeChanged,
    required this.onTogglePassword,
    required this.loginFormKey,
    required this.registerFormKey,
    required this.usernameController,
    required this.passwordController,
    required this.pharmacyNameController,
    required this.registerUsernameController,
    required this.registerPasswordController,
    required this.phoneController,
    required this.addressController,
    required this.onLogin,
    required this.onRegister,
  });

  final _AuthMode mode;
  final bool loading;
  final String? error;
  final String? successMessage;
  final bool obscurePassword;
  final ValueChanged<_AuthMode> onModeChanged;
  final VoidCallback onTogglePassword;
  final GlobalKey<FormState> loginFormKey;
  final GlobalKey<FormState> registerFormKey;
  final TextEditingController usernameController;
  final TextEditingController passwordController;
  final TextEditingController pharmacyNameController;
  final TextEditingController registerUsernameController;
  final TextEditingController registerPasswordController;
  final TextEditingController phoneController;
  final TextEditingController addressController;
  final VoidCallback onLogin;
  final VoidCallback onRegister;

  @override
  Widget build(BuildContext context) {
    final colors = Theme.of(context).extension<AppThemeColors>()!;
    final textTheme = Theme.of(context).textTheme;

    return Container(
      padding: const EdgeInsets.all(AppSpacing.xl),
      decoration: BoxDecoration(
        color: colors.surface,
        border: AppBorders.micro(colors),
        borderRadius: AppRadii.panel,
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        mainAxisSize: MainAxisSize.min,
        children: [
          Text(
            mode == _AuthMode.login ? 'تسجيل الدخول' : 'إنشاء حساب صيدلية',
            style: textTheme.headlineMedium?.copyWith(
              color: colors.textPrimary,
              fontWeight: FontWeight.w900,
            ),
          ),
          const SizedBox(height: AppSpacing.lg),
          _ModeSwitch(mode: mode, onChanged: onModeChanged),
          const SizedBox(height: AppSpacing.xl),
          AnimatedSwitcher(
            duration: const Duration(milliseconds: 180),
            child: mode == _AuthMode.login
                ? _LoginForm(
                    key: const ValueKey('login-form'),
                    formKey: loginFormKey,
                    usernameController: usernameController,
                    passwordController: passwordController,
                    obscurePassword: obscurePassword,
                    onTogglePassword: onTogglePassword,
                    onSubmit: onLogin,
                  )
                : _RegisterForm(
                    key: const ValueKey('register-form'),
                    formKey: registerFormKey,
                    pharmacyNameController: pharmacyNameController,
                    usernameController: registerUsernameController,
                    passwordController: registerPasswordController,
                    phoneController: phoneController,
                    addressController: addressController,
                    obscurePassword: obscurePassword,
                    onTogglePassword: onTogglePassword,
                    onSubmit: onRegister,
                  ),
          ),
          if (error != null) ...[
            const SizedBox(height: AppSpacing.lg),
            _MessageBanner(
              text: error!,
              icon: Icons.error_rounded,
              color: colors.danger,
            ),
          ],
          if (successMessage != null) ...[
            const SizedBox(height: AppSpacing.lg),
            _MessageBanner(
              text: successMessage!,
              icon: Icons.check_circle_rounded,
              color: colors.emerald,
            ),
          ],
          const SizedBox(height: AppSpacing.xl),
          ElevatedButton.icon(
            onPressed: loading
                ? null
                : mode == _AuthMode.login
                ? onLogin
                : onRegister,
            icon: loading
                ? SizedBox(
                    width: 18,
                    height: 18,
                    child: CircularProgressIndicator(
                      strokeWidth: 2,
                      color: colors.background,
                    ),
                  )
                : Icon(
                    mode == _AuthMode.login
                        ? Icons.login_rounded
                        : Icons.person_add_alt_1_rounded,
                    size: 20,
                  ),
            label: Text(
              mode == _AuthMode.login ? 'دخول النظام' : 'إرسال طلب التسجيل',
            ),
          ),
        ],
      ),
    );
  }
}

class _ModeSwitch extends StatelessWidget {
  const _ModeSwitch({required this.mode, required this.onChanged});

  final _AuthMode mode;
  final ValueChanged<_AuthMode> onChanged;

  @override
  Widget build(BuildContext context) {
    final colors = Theme.of(context).extension<AppThemeColors>()!;

    return Container(
      padding: const EdgeInsets.all(AppSpacing.xs),
      decoration: BoxDecoration(
        color: colors.surfaceElevated,
        border: AppBorders.micro(colors),
        borderRadius: AppRadii.card,
      ),
      child: Row(
        children: [
          Expanded(
            child: _ModeButton(
              label: 'دخول',
              selected: mode == _AuthMode.login,
              onPressed: () => onChanged(_AuthMode.login),
            ),
          ),
          Expanded(
            child: _ModeButton(
              label: 'تسجيل',
              selected: mode == _AuthMode.register,
              onPressed: () => onChanged(_AuthMode.register),
            ),
          ),
        ],
      ),
    );
  }
}

class _ModeButton extends StatelessWidget {
  const _ModeButton({
    required this.label,
    required this.selected,
    required this.onPressed,
  });

  final String label;
  final bool selected;
  final VoidCallback onPressed;

  @override
  Widget build(BuildContext context) {
    final colors = Theme.of(context).extension<AppThemeColors>()!;

    return TextButton(
      onPressed: onPressed,
      style: TextButton.styleFrom(
        backgroundColor: selected ? colors.surfaceHover : Colors.transparent,
        foregroundColor: selected ? colors.cyanBright : colors.textMuted,
      ),
      child: Text(label),
    );
  }
}

class _LoginForm extends StatelessWidget {
  const _LoginForm({
    super.key,
    required this.formKey,
    required this.usernameController,
    required this.passwordController,
    required this.obscurePassword,
    required this.onTogglePassword,
    required this.onSubmit,
  });

  final GlobalKey<FormState> formKey;
  final TextEditingController usernameController;
  final TextEditingController passwordController;
  final bool obscurePassword;
  final VoidCallback onTogglePassword;
  final VoidCallback onSubmit;

  @override
  Widget build(BuildContext context) {
    return Form(
      key: formKey,
      child: Column(
        children: [
          _AuthTextField(
            controller: usernameController,
            label: 'اسم المستخدم',
            icon: Icons.person_rounded,
            validator: _required,
            textInputAction: TextInputAction.next,
          ),
          const SizedBox(height: AppSpacing.lg),
          _PasswordField(
            controller: passwordController,
            obscurePassword: obscurePassword,
            onTogglePassword: onTogglePassword,
            onSubmit: onSubmit,
          ),
        ],
      ),
    );
  }
}

class _RegisterForm extends StatelessWidget {
  const _RegisterForm({
    super.key,
    required this.formKey,
    required this.pharmacyNameController,
    required this.usernameController,
    required this.passwordController,
    required this.phoneController,
    required this.addressController,
    required this.obscurePassword,
    required this.onTogglePassword,
    required this.onSubmit,
  });

  final GlobalKey<FormState> formKey;
  final TextEditingController pharmacyNameController;
  final TextEditingController usernameController;
  final TextEditingController passwordController;
  final TextEditingController phoneController;
  final TextEditingController addressController;
  final bool obscurePassword;
  final VoidCallback onTogglePassword;
  final VoidCallback onSubmit;

  @override
  Widget build(BuildContext context) {
    return Form(
      key: formKey,
      child: Column(
        children: [
          _AuthTextField(
            controller: pharmacyNameController,
            label: 'اسم الصيدلية',
            icon: Icons.local_pharmacy_rounded,
            validator: _required,
            textInputAction: TextInputAction.next,
          ),
          const SizedBox(height: AppSpacing.lg),
          _AuthTextField(
            controller: usernameController,
            label: 'اسم المستخدم',
            icon: Icons.person_rounded,
            validator: _required,
            textInputAction: TextInputAction.next,
          ),
          const SizedBox(height: AppSpacing.lg),
          _PasswordField(
            controller: passwordController,
            obscurePassword: obscurePassword,
            onTogglePassword: onTogglePassword,
            onSubmit: onSubmit,
            validator: (value) {
              if ((value ?? '').trim().length < 8) {
                return 'كلمة المرور لا تقل عن 8 أحرف';
              }
              return null;
            },
          ),
          const SizedBox(height: AppSpacing.lg),
          _AuthTextField(
            controller: phoneController,
            label: 'رقم الهاتف',
            icon: Icons.phone_rounded,
            validator: _required,
            keyboardType: TextInputType.phone,
            textInputAction: TextInputAction.next,
          ),
          const SizedBox(height: AppSpacing.lg),
          _AuthTextField(
            controller: addressController,
            label: 'العنوان',
            icon: Icons.location_on_rounded,
            textInputAction: TextInputAction.done,
            onSubmitted: (_) => onSubmit(),
          ),
        ],
      ),
    );
  }
}

class _AuthTextField extends StatelessWidget {
  const _AuthTextField({
    required this.controller,
    required this.label,
    required this.icon,
    this.validator,
    this.keyboardType,
    this.textInputAction,
    this.onSubmitted,
  });

  final TextEditingController controller;
  final String label;
  final IconData icon;
  final FormFieldValidator<String>? validator;
  final TextInputType? keyboardType;
  final TextInputAction? textInputAction;
  final ValueChanged<String>? onSubmitted;

  @override
  Widget build(BuildContext context) {
    final colors = Theme.of(context).extension<AppThemeColors>()!;

    return TextFormField(
      controller: controller,
      validator: validator,
      keyboardType: keyboardType,
      textInputAction: textInputAction,
      onFieldSubmitted: onSubmitted,
      decoration: InputDecoration(
        labelText: label,
        prefixIcon: Icon(icon, color: colors.textMuted, size: 20),
      ),
    );
  }
}

class _PasswordField extends StatelessWidget {
  const _PasswordField({
    required this.controller,
    required this.obscurePassword,
    required this.onTogglePassword,
    required this.onSubmit,
    this.validator,
  });

  final TextEditingController controller;
  final bool obscurePassword;
  final VoidCallback onTogglePassword;
  final VoidCallback onSubmit;
  final FormFieldValidator<String>? validator;

  @override
  Widget build(BuildContext context) {
    final colors = Theme.of(context).extension<AppThemeColors>()!;

    return TextFormField(
      controller: controller,
      obscureText: obscurePassword,
      validator: validator ?? _required,
      textInputAction: TextInputAction.done,
      onFieldSubmitted: (_) => onSubmit(),
      decoration: InputDecoration(
        labelText: 'كلمة المرور',
        prefixIcon: Icon(Icons.lock_rounded, color: colors.textMuted, size: 20),
        suffixIcon: IconButton(
          tooltip: obscurePassword ? 'إظهار' : 'إخفاء',
          onPressed: onTogglePassword,
          icon: Icon(
            obscurePassword
                ? Icons.visibility_rounded
                : Icons.visibility_off_rounded,
            size: 20,
          ),
        ),
      ),
    );
  }
}

class _MessageBanner extends StatelessWidget {
  const _MessageBanner({
    required this.text,
    required this.icon,
    required this.color,
  });

  final String text;
  final IconData icon;
  final Color color;

  @override
  Widget build(BuildContext context) {
    final colors = Theme.of(context).extension<AppThemeColors>()!;
    final textTheme = Theme.of(context).textTheme;

    return Container(
      padding: const EdgeInsets.all(AppSpacing.md),
      decoration: BoxDecoration(
        color: color.withValues(alpha: 0.12),
        border: Border.all(color: color.withValues(alpha: 0.24)),
        borderRadius: AppRadii.card,
      ),
      child: Row(
        children: [
          Icon(icon, color: color, size: 20),
          const SizedBox(width: AppSpacing.sm),
          Expanded(
            child: Text(
              text,
              style: textTheme.bodyMedium?.copyWith(color: colors.textPrimary),
            ),
          ),
        ],
      ),
    );
  }
}

class _SignalChip extends StatelessWidget {
  const _SignalChip({
    required this.icon,
    required this.label,
    required this.color,
  });

  final IconData icon;
  final String label;
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

class _MiniMetric extends StatelessWidget {
  const _MiniMetric({
    required this.label,
    required this.value,
    required this.color,
  });

  final String label;
  final String value;
  final Color color;

  @override
  Widget build(BuildContext context) {
    final colors = Theme.of(context).extension<AppThemeColors>()!;
    final textTheme = Theme.of(context).textTheme;

    return Expanded(
      child: Container(
        padding: const EdgeInsets.all(AppSpacing.lg),
        decoration: BoxDecoration(
          color: colors.surfaceElevated,
          border: AppBorders.micro(colors),
          borderRadius: AppRadii.card,
        ),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(label, style: textTheme.labelMedium),
            const SizedBox(height: AppSpacing.sm),
            Text(
              value,
              style: textTheme.headlineMedium?.copyWith(
                color: color,
                fontWeight: FontWeight.w900,
              ),
            ),
          ],
        ),
      ),
    );
  }
}

String? _required(String? value) {
  if (value == null || value.trim().isEmpty) return 'هذا الحقل مطلوب';
  return null;
}
