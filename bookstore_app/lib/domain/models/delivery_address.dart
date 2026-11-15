class DeliveryAddress {
  const DeliveryAddress({
    required this.fullName,
    required this.addressLine,
    required this.city,
    required this.postalCode,
    required this.phone,
  });

  final String fullName;
  final String addressLine;
  final String city;
  final String postalCode;
  final String phone;
}
