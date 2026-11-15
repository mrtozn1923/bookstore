/// Field validators for TextFormField: null means valid, a string is the error.
abstract final class Validators {
  static final _email = RegExp(r'^[^@\s]+@[^@\s]+\.[^@\s]+$');
  static final _postalCode = RegExp(r'^\d{5}$');

  static String? required(String? value) =>
      value == null || value.trim().isEmpty ? 'This field is required' : null;

  static String? email(String? value) =>
      required(value) ??
      (_email.hasMatch(value!.trim()) ? null : 'Enter a valid email address');

  static String? password(String? value) =>
      required(value) ??
      (value!.length >= 8 ? null : 'Use at least 8 characters');

  /// Turkish postal codes have five digits.
  static String? postalCode(String? value) =>
      required(value) ??
      (_postalCode.hasMatch(value!.trim()) ? null : 'Postal code has 5 digits');

  static String? phone(String? value) {
    final digits = value?.replaceAll(RegExp(r'\D'), '') ?? '';
    if (digits.isEmpty) return 'This field is required';
    return digits.length == 10 || digits.length == 11
        ? null
        : 'Enter a 10 or 11 digit phone number';
  }
}
