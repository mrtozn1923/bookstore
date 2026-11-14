import { Routes } from '@angular/router';
import { AdminComponent } from './admin.component';
import { CustomerListComponent } from '../customer-list/customer-list.component';

export const ADMIN_ROUTES: Routes = [
  { path: '', component: AdminComponent, title: 'Admin' },
  { path: 'customers', component: CustomerListComponent, title: 'Customers' }
];
