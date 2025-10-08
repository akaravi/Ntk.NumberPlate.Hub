import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ApiService, Statistics, VehicleDetection } from '../../services/api.service';

@Component({
    selector: 'app-dashboard',
    standalone: true,
    imports: [CommonModule],
    template: `
    <div class="dashboard">
      <h2 class="page-title">داشبورد</h2>

      <!-- آمار -->
      <div class="stats-grid" *ngIf="statistics">
        <div class="stat-card">
          <div class="stat-icon">📊</div>
          <div class="stat-info">
            <div class="stat-value">{{ statistics.totalDetections }}</div>
            <div class="stat-label">کل تشخیص‌ها</div>
          </div>
        </div>

        <div class="stat-card">
          <div class="stat-icon">📅</div>
          <div class="stat-info">
            <div class="stat-value">{{ statistics.todayDetections }}</div>
            <div class="stat-label">تشخیص‌های امروز</div>
          </div>
        </div>

        <div class="stat-card danger">
          <div class="stat-icon">⚠️</div>
          <div class="stat-info">
            <div class="stat-value">{{ statistics.speedViolations }}</div>
            <div class="stat-label">تخلفات سرعت</div>
          </div>
        </div>

        <div class="stat-card success">
          <div class="stat-icon">🖥️</div>
          <div class="stat-info">
            <div class="stat-value">{{ statistics.activeNodes }}</div>
            <div class="stat-label">نودهای فعال</div>
          </div>
        </div>
      </div>

      <!-- آخرین تشخیص‌ها -->
      <div class="card">
        <h3 class="card-title">آخرین تشخیص‌ها</h3>
        <div class="loading" *ngIf="loading">
          <div class="spinner"></div>
        </div>
        <div *ngIf="!loading && recentDetections.length > 0">
          <table class="table">
            <thead>
              <tr>
                <th>پلاک</th>
                <th>نود</th>
                <th>سرعت</th>
                <th>زمان</th>
                <th>وضعیت</th>
              </tr>
            </thead>
            <tbody>
              <tr *ngFor="let detection of recentDetections.slice(0, 10)">
                <td><strong>{{ detection.plateNumber }}</strong></td>
                <td>{{ detection.nodeName }}</td>
                <td>{{ detection.speed.toFixed(1) }} km/h</td>
                <td>{{ formatDate(detection.detectionTime) }}</td>
                <td>
                  <span class="badge" [class.badge-danger]="detection.isSpeedViolation" [class.badge-success]="!detection.isSpeedViolation">
                    {{ detection.isSpeedViolation ? 'تخلف' : 'عادی' }}
                  </span>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
        <div *ngIf="!loading && recentDetections.length === 0" class="no-data">
          هیچ تشخیصی یافت نشد
        </div>
      </div>
    </div>
  `,
    styles: [`
    .dashboard {
      padding: 20px 0;
    }

    .page-title {
      font-size: 28px;
      font-weight: 700;
      margin-bottom: 30px;
      color: #323130;
    }

    .stats-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
      gap: 20px;
      margin-bottom: 30px;
    }

    .stat-card {
      background: white;
      border-radius: 12px;
      padding: 24px;
      display: flex;
      align-items: center;
      gap: 20px;
      box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
      transition: transform 0.3s ease;
    }

    .stat-card:hover {
      transform: translateY(-4px);
      box-shadow: 0 4px 16px rgba(0, 0, 0, 0.15);
    }

    .stat-card.danger {
      background: linear-gradient(135deg, #ff6b6b 0%, #ee5a6f 100%);
      color: white;
    }

    .stat-card.success {
      background: linear-gradient(135deg, #51cf66 0%, #37b24d 100%);
      color: white;
    }

    .stat-icon {
      font-size: 48px;
    }

    .stat-info {
      flex: 1;
    }

    .stat-value {
      font-size: 32px;
      font-weight: 700;
      margin-bottom: 4px;
    }

    .stat-label {
      font-size: 14px;
      opacity: 0.9;
    }

    .no-data {
      text-align: center;
      padding: 40px;
      color: #666;
    }

    @media (max-width: 768px) {
      .stats-grid {
        grid-template-columns: 1fr;
      }

      .stat-card {
        padding: 20px;
      }

      .stat-value {
        font-size: 28px;
      }
    }
  `]
})
export class DashboardComponent implements OnInit {
    statistics: Statistics | null = null;
    recentDetections: VehicleDetection[] = [];
    loading = true;

    constructor(private apiService: ApiService) { }

    ngOnInit() {
        this.loadStatistics();
        this.loadRecentDetections();

        // به‌روزرسانی خودکار هر 10 ثانیه
        setInterval(() => {
            this.loadStatistics();
            this.loadRecentDetections();
        }, 10000);
    }

    loadStatistics() {
        this.apiService.getStatistics().subscribe({
            next: (response) => {
                if (response.success) {
                    this.statistics = response.data;
                }
            },
            error: (error) => {
                console.error('خطا در دریافت آمار:', error);
            }
        });
    }

    loadRecentDetections() {
        this.apiService.getRecentDetections(50).subscribe({
            next: (response) => {
                if (response.success) {
                    this.recentDetections = response.data;
                }
                this.loading = false;
            },
            error: (error) => {
                console.error('خطا در دریافت تشخیص‌ها:', error);
                this.loading = false;
            }
        });
    }

    formatDate(date: Date): string {
        const d = new Date(date);
        return d.toLocaleString('fa-IR');
    }
}


