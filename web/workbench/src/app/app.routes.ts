import { Routes } from '@angular/router';
import { Alerts } from './pages/alerts';
import { Dashboard } from './pages/dashboard';
import { MerchantDetail } from './pages/merchant-detail';

export const routes: Routes = [
  { path: '', component: Dashboard },
  { path: 'alerts', component: Alerts },
  { path: 'merchants/:id', component: MerchantDetail }
];
