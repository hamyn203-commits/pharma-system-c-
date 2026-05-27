import 'package:al_neda_admin_flutter/core/config/app_config.dart';
import 'package:dio/dio.dart';
import 'package:flutter/foundation.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:pretty_dio_logger/pretty_dio_logger.dart';

final class ApiClient {
  ApiClient({
    required this.dotnetApi,
    required this.analyticsApi,
    required FlutterSecureStorage secureStorage,
  }) : _secureStorage = secureStorage;

  factory ApiClient.local({FlutterSecureStorage? secureStorage}) {
    final storage = secureStorage ?? const FlutterSecureStorage();
    return ApiClient(
      dotnetApi: _buildDio(
        baseUrl: AppConfig.dotnetApiBaseUrl,
        secureStorage: storage,
      ),
      analyticsApi: _buildDio(baseUrl: AppConfig.analyticsApiBaseUrl),
      secureStorage: storage,
    );
  }

  final Dio dotnetApi;
  final Dio analyticsApi;
  final FlutterSecureStorage _secureStorage;

  Future<void> saveAuthToken(String token) {
    return _secureStorage.write(
      key: AppConfig.authTokenStorageKey,
      value: token,
    );
  }

  Future<String?> readAuthToken() {
    return _secureStorage.read(key: AppConfig.authTokenStorageKey);
  }

  Future<void> clearAuthToken() {
    return _secureStorage.delete(key: AppConfig.authTokenStorageKey);
  }

  static Dio _buildDio({
    required String baseUrl,
    FlutterSecureStorage? secureStorage,
  }) {
    final dio = Dio(
      BaseOptions(
        baseUrl: baseUrl,
        connectTimeout: const Duration(seconds: 10),
        receiveTimeout: const Duration(seconds: 20),
        sendTimeout: const Duration(seconds: 20),
        contentType: Headers.jsonContentType,
        responseType: ResponseType.json,
      ),
    );

    if (secureStorage != null) {
      dio.interceptors.add(
        QueuedInterceptorsWrapper(
          onRequest: (options, handler) async {
            final token = await secureStorage.read(
              key: AppConfig.authTokenStorageKey,
            );
            if (token != null && token.isNotEmpty) {
              options.headers['Authorization'] = 'Bearer $token';
            }
            handler.next(options);
          },
        ),
      );
    }

    if (kDebugMode) {
      dio.interceptors.add(
        PrettyDioLogger(
          requestHeader: false,
          requestBody: true,
          responseHeader: false,
          responseBody: false,
          compact: true,
        ),
      );
    }

    return dio;
  }
}
