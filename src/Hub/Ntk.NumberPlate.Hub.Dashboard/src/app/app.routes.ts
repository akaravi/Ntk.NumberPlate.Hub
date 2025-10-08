import { Routes } from '@angular/router';
import { DashboardComponent } from './pages/dashboard/dashboard.component';
import { DetectionsComponent } from './pages/detections/detections.component';
import { NodesComponent } from './pages/nodes/nodes.component';

export const routes: Routes = [
    { path: '', redirectTo: '/dashboard', pathMatch: 'full' },
    { path: 'dashboard', component: DashboardComponent },
    { path: 'detections', component: DetectionsComponent },
    { path: 'nodes', component: NodesComponent },
    { path: '**', redirectTo: '/dashboard' }
];


