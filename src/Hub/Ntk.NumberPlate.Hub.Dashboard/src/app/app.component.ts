import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterOutlet } from '@angular/router';
import { HeaderComponent } from './components/header/header.component';

@Component({
    selector: 'app-root',
    standalone: true,
    imports: [CommonModule, RouterOutlet, HeaderComponent],
    template: `
    <app-header></app-header>
    <main class="main-content">
      <router-outlet></router-outlet>
    </main>
  `,
    styles: [`
    .main-content {
      padding: 20px;
      max-width: 1400px;
      margin: 0 auto;
    }
  `]
})
export class AppComponent {
    title = 'داشبورد تشخیص پلاک خودرو';
}


