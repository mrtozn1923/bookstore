import 'package:go_router/go_router.dart';
import 'package:material_ui/material_ui.dart';
import 'package:provider/provider.dart';

import '../../cart/view_models/cart_view_model.dart';

/// The bottom navigation around the three tabs; each tab keeps its own stack.
class ScaffoldWithNavBar extends StatelessWidget {
  const ScaffoldWithNavBar({super.key, required this.navigationShell});

  final StatefulNavigationShell navigationShell;

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: navigationShell,
      bottomNavigationBar: NavigationBar(
        selectedIndex: navigationShell.currentIndex,
        // Tapping the current tab again goes back to its first page
        onDestinationSelected: (index) => navigationShell.goBranch(
          index,
          initialLocation: index == navigationShell.currentIndex,
        ),
        destinations: [
          const NavigationDestination(
            icon: Icon(Icons.menu_book_outlined),
            label: 'Catalog',
          ),
          NavigationDestination(
            icon: Selector<CartViewModel, int>(
              selector: (_, cart) => cart.itemCount,
              builder: (context, count, _) => Badge.count(
                count: count,
                isLabelVisible: count > 0,
                child: const Icon(Icons.shopping_cart_outlined),
              ),
            ),
            label: 'Cart',
          ),
          const NavigationDestination(
            icon: Icon(Icons.receipt_long_outlined),
            label: 'Orders',
          ),
        ],
      ),
    );
  }
}
