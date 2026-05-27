import 'package:al_neda_admin_flutter/core/network/api_client.dart';

final class AuthApi {
  const AuthApi(this._client);

  final ApiClient _client;

  Future<LoginResponseDto> login({
    required String username,
    required String password,
  }) async {
    final response = await _client.dotnetApi.post<Map<String, dynamic>>(
      '/api/auth/login',
      data: {'username': username, 'password': password},
    );
    final result = LoginResponseDto.fromJson(response.data ?? {});
    await _client.saveAuthToken(result.token);
    return result;
  }

  Future<bool> validateToken() async {
    final response = await _client.dotnetApi.post<Map<String, dynamic>>(
      '/api/auth/validate',
    );
    return response.data?['valid'] == true;
  }

  Future<RegisterPharmacyResponseDto> registerPharmacy({
    required String pharmacyName,
    required String username,
    required String password,
    required String phone,
    String? address,
  }) async {
    final response = await _client.dotnetApi.post<Map<String, dynamic>>(
      '/api/auth/register/pharmacy',
      data: {
        'pharmacyName': pharmacyName,
        'username': username,
        'password': password,
        'phone': phone,
        if (address != null && address.trim().isNotEmpty)
          'address': address.trim(),
      },
    );
    return RegisterPharmacyResponseDto.fromJson(response.data ?? {});
  }

  Future<void> logout() {
    return _client.clearAuthToken();
  }
}

final class LoginResponseDto {
  const LoginResponseDto({
    required this.id,
    required this.username,
    required this.role,
    required this.token,
    this.pharmacyId,
    this.pharmacyName,
    this.user,
  });

  factory LoginResponseDto.fromJson(Map<String, dynamic> json) {
    return LoginResponseDto(
      id: _int(json['id']),
      username: _string(json['username']),
      role: _string(json['role']),
      token: _string(json['token']),
      pharmacyId: _nullableInt(json['pharmacyId']),
      pharmacyName: _nullableString(json['pharmacyName']),
      user: json['user'] is Map<String, dynamic>
          ? LoginUserDto.fromJson(json['user'] as Map<String, dynamic>)
          : null,
    );
  }

  final int id;
  final String username;
  final String role;
  final int? pharmacyId;
  final String? pharmacyName;
  final String token;
  final LoginUserDto? user;
}

final class LoginUserDto {
  const LoginUserDto({
    required this.id,
    required this.username,
    required this.role,
    this.pharmacyId,
    this.pharmacyName,
  });

  factory LoginUserDto.fromJson(Map<String, dynamic> json) {
    return LoginUserDto(
      id: _int(json['id']),
      username: _string(json['username']),
      role: _string(json['role']),
      pharmacyId: _nullableInt(json['pharmacyId']),
      pharmacyName: _nullableString(json['pharmacyName']),
    );
  }

  final int id;
  final String username;
  final String role;
  final int? pharmacyId;
  final String? pharmacyName;
}

final class RegisterPharmacyResponseDto {
  const RegisterPharmacyResponseDto({
    required this.pharmacyId,
    required this.userId,
    required this.username,
    required this.pharmacyName,
    required this.accountStatus,
    required this.requiresApproval,
    required this.message,
  });

  factory RegisterPharmacyResponseDto.fromJson(Map<String, dynamic> json) {
    return RegisterPharmacyResponseDto(
      pharmacyId: _int(json['pharmacyId']),
      userId: _int(json['userId']),
      username: _string(json['username']),
      pharmacyName: _string(json['pharmacyName']),
      accountStatus: _string(json['accountStatus']),
      requiresApproval: json['requiresApproval'] == true,
      message: _string(json['message']),
    );
  }

  final int pharmacyId;
  final int userId;
  final String username;
  final String pharmacyName;
  final String accountStatus;
  final bool requiresApproval;
  final String message;
}

int _int(Object? value) {
  if (value is int) return value;
  if (value is num) return value.toInt();
  return int.tryParse('$value') ?? 0;
}

int? _nullableInt(Object? value) {
  if (value == null) return null;
  return _int(value);
}

String _string(Object? value) => value?.toString() ?? '';

String? _nullableString(Object? value) {
  final text = value?.toString();
  return text == null || text.isEmpty ? null : text;
}
