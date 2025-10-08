import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService, VehicleDetection } from '../../services/api.service';

@Component({
    selector: 'app-detections',
    standalone: true,
    imports: [CommonModule, FormsModule],
    template: `
    <div class="detections-page">
      <h2 class="page-title">لیست تشخیص‌ها</h2>

      <!-- فیلتر و جستجو -->
      <div class="card filters">
        <div class="filter-group">
          <label>جستجوی پلاک:</label>
          <input type="text" [(ngModel)]="searchPlate" (input)="onSearch()" placeholder="مثال: 12ایران345-67">
        </div>
        <div class="filter-group">
          <label>فیلتر:</label>
          <select [(ngModel)]="filterType" (change)="onFilterChange()">
            <option value="all">همه</option>
            <option value="violation">تخلفات</option>
            <option value="normal">عادی</option>
          </select>
        </div>
      </div>

      <!-- جدول -->
      <div class="card">
        <div class="loading" *ngIf="loading">
          <div class="spinner"></div>
        </div>
        <div *ngIf="!loading">
          <table class="table" *ngIf="filteredDetections.length > 0">
            <thead>
              <tr>
                <th>پلاک</th>
                <th>نود</th>
                <th>سرعت</th>
                <th>حد مجاز</th>
                <th>نوع خودرو</th>
                <th>اعتماد</th>
                <th>زمان</th>
                <th>وضعیت</th>
              </tr>
            </thead>
            <tbody>
              <tr *ngFor="let detection of filteredDetections">
                <td><strong>{{ detection.plateNumber }}</strong></td>
                <td>{{ detection.nodeName }}</td>
                <td>{{ detection.speed.toFixed(1) }} km/h</td>
                <td>{{ detection.speedLimit }} km/h</td>
                <td>{{ getVehicleTypeName(detection.vehicleType) }}</td>
                <td>{{ (detection.confidence * 100).toFixed(0) }}%</td>
                <td>{{ formatDate(detection.detectionTime) }}</td>
                <td>
                  <span class="badge" [class.badge-danger]="detection.isSpeedViolation" [class.badge-success]="!detection.isSpeedViolation">
                    {{ detection.isSpeedViolation ? '⚠️ تخلف سرعت' : '✓ عادی' }}
                  </span>
                </td>
              </tr>
            </tbody>
          </table>
          <div *ngIf="filteredDetections.length === 0" class="no-data">
            هیچ تشخیصی یافت نشد
          </div>
        </div>
      </div>

      <!-- صفحه‌بندی -->
      <div class="pagination" *ngIf="filteredDetections.length > pageSize">
        <button (click)="previousPage()" [disabled]="currentPage === 1">قبلی</button>
        <span>صفحه {{ currentPage }} از {{ totalPages }}</span>
        <button (click)="nextPage()" [disabled]="currentPage === totalPages">بعدی</button>
      </div>
    </div>
  `,
    styles: [`
    .detections-page {
      padding: 20px 0;
    }

    .page-title {
      font-size: 28px;
      font-weight: 700;
      margin-bottom: 30px;
      color: #323130;
    }

    .filters {
      display: flex;
      gap: 20px;
      margin-bottom: 20px;
      flex-wrap: wrap;
    }

    .filter-group {
      flex: 1;
      min-width: 200px;
    }

    .filter-group label {
      display: block;
      margin-bottom: 8px;
      font-weight: 600;
      color: #323130;
    }

    .filter-group input,
    .filter-group select {
      width: 100%;
      padding: 10px;
      border: 1px solid #e1e1e1;
      border-radius: 4px;
      font-size: 14px;
    }

    .filter-group input:focus,
    .filter-group select:focus {
      outline: none;
      border-color: #0078d4;
    }

    .pagination {
      display: flex;
      justify-content: center;
      align-items: center;
      gap: 20px;
      margin-top: 20px;
    }

    .pagination button {
      padding: 8px 16px;
      background: #0078d4;
      color: white;
      border: none;
      border-radius: 4px;
      cursor: pointer;
      font-weight: 600;
    }

    .pagination button:disabled {
      background: #ccc;
      cursor: not-allowed;
    }

    .pagination span {
      font-weight: 600;
    }

    .no-data {
      text-align: center;
      padding: 40px;
      color: #666;
    }
  `]
})
export class DetectionsComponent implements OnInit {
    allDetections: VehicleDetection[] = [];
    filteredDetections: VehicleDetection[] = [];
    loading = true;
    searchPlate = '';
    filterType = 'all';
    currentPage = 1;
    pageSize = 20;
    totalPages = 1;

    constructor(private apiService: ApiService) { }

    ngOnInit() {
        this.loadDetections();
    }

    loadDetections() {
        this.apiService.getRecentDetections(1000).subscribe({
            next: (response) => {
                if (response.success) {
                    this.allDetections = response.data;
                    this.applyFilters();
                }
                this.loading = false;
            },
            error: (error) => {
                console.error('خطا در دریافت تشخیص‌ها:', error);
                this.loading = false;
            }
        });
    }

    onSearch() {
        this.applyFilters();
    }

    onFilterChange() {
        this.applyFilters();
    }

    applyFilters() {
        let filtered = [...this.allDetections];

        // فیلتر پلاک
        if (this.searchPlate) {
            filtered = filtered.filter(d =>
                d.plateNumber.includes(this.searchPlate)
            );
        }

        // فیلتر نوع
        if (this.filterType === 'violation') {
            filtered = filtered.filter(d => d.isSpeedViolation);
        } else if (this.filterType === 'normal') {
            filtered = filtered.filter(d => !d.isSpeedViolation);
        }

        this.filteredDetections = filtered;
        this.totalPages = Math.ceil(filtered.length / this.pageSize);
        this.currentPage = 1;
    }

    previousPage() {
        if (this.currentPage > 1) {
            this.currentPage--;
        }
    }

    nextPage() {
        if (this.currentPage < this.totalPages) {
            this.currentPage++;
        }
    }

    getVehicleTypeName(type: number): string {
        const types: { [key: number]: string } = {
            0: 'نامشخص',
            1: 'خودرو',
            2: 'موتورسیکلت',
            3: 'کامیون',
            4: 'اتوبوس',
            5: 'ون'
        };
        return types[type] || 'نامشخص';
    }

    formatDate(date: Date): string {
        const d = new Date(date);
        return d.toLocaleString('fa-IR');
    }
}


