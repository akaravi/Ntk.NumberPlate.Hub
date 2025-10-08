import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';

@Component({
    selector: 'app-header',
    standalone: true,
    imports: [CommonModule, RouterModule],
    template: `
    <header class="header">
      <div class="container">
        <h1 class="logo">🚗 سامانه تشخیص پلاک و سرعت خودرو</h1>
        <nav class="nav">
          <a routerLink="/dashboard" routerLinkActive="active">داشبورد</a>
          <a routerLink="/detections" routerLinkActive="active">تشخیص‌ها</a>
          <a routerLink="/nodes" routerLinkActive="active">نودها</a>
        </nav>
      </div>
    </header>
  `,
    styles: [`
    .header {
      background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
      color: white;
      padding: 20px 0;
      box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
    }

    .container {
      max-width: 1400px;
      margin: 0 auto;
      padding: 0 20px;
      display: flex;
      justify-content: space-between;
      align-items: center;
    }

    .logo {
      font-size: 24px;
      font-weight: 700;
      margin: 0;
    }

    .nav {
      display: flex;
      gap: 20px;
    }

    .nav a {
      color: white;
      text-decoration: none;
      padding: 8px 16px;
      border-radius: 4px;
      transition: all 0.3s ease;
      font-weight: 500;
    }

    .nav a:hover {
      background: rgba(255, 255, 255, 0.2);
    }

    .nav a.active {
      background: rgba(255, 255, 255, 0.3);
    }

    @media (max-width: 768px) {
      .container {
        flex-direction: column;
        gap: 15px;
      }

      .logo {
        font-size: 20px;
      }

      .nav {
        gap: 10px;
      }

      .nav a {
        padding: 6px 12px;
        font-size: 14px;
      }
    }
  `]
})
export class HeaderComponent { }


