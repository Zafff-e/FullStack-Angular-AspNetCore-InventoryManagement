import { Routes } from '@angular/router';
import { DisplayProducts } from './components/display-products/display-products';
import { AddProduct } from './components/add-product/add-product';
import { CategoriesComponent } from './components/categories/categories';
import { LoginComponent } from './components/login/login';
import { RegisterComponent } from './components/register/register';
import { authGuard } from './guards/auth-guard';

export const routes: Routes = [
  { path: '', redirectTo: 'display-products', pathMatch: 'full' },
  { path: 'display-products', component: DisplayProducts },
  { path: 'add-product', component: AddProduct, canActivate: [authGuard] },
  { path: 'edit-product/:id', component: AddProduct, canActivate: [authGuard] },
  { path: 'categories', component: CategoriesComponent },
  { path: 'login', component: LoginComponent },
  { path: 'register', component: RegisterComponent },
  { path: '**', redirectTo: 'display-products' }
];
