import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ApiService, NodeInfo } from '../../services/api.service';

@Component({
    selector: 'app-nodes',
    standalone: true,
    imports: [CommonModule],
    template: `
    <div class="nodes-page">
      <h2 class="page-title">مدیریت نودها</h2>

      <!-- آمار نودها -->
      <div class="stats-row">
        <div class="stat-box online">
          <div class="stat-number">{{ onlineNodesCount }}</div>
          <div class="stat-text">نودهای آنلاین</div>
        </div>
        <div class="stat-box offline">
          <div class="stat-number">{{ offlineNodesCount }}</div>
          <div class="stat-text">نودهای آفلاین</div>
        </div>
        <div class="stat-box total">
          <div class="stat-number">{{ nodes.length }}</div>
          <div class="stat-text">کل نودها</div>
        </div>
      </div>

      <!-- لیست نودها -->
      <div class="card">
        <h3 class="card-title">لیست نودها</h3>
        <div class="loading" *ngIf="loading">
          <div class="spinner"></div>
        </div>
        <div *ngIf="!loading">
          <div class="nodes-grid" *ngIf="nodes.length > 0">
            <div class="node-card" *ngFor="let node of nodes" [class.online]="node.isOnline">
              <div class="node-header">
                <h4>{{ node.nodeName }}</h4>
                <span class="status-badge" [class.online]="node.isOnline">
                  {{ node.isOnline ? '🟢 آنلاین' : '🔴 آفلاین' }}
                </span>
              </div>
              <div class="node-details">
                <div class="detail-row">
                  <span class="label">شناسه:</span>
                  <span class="value">{{ node.nodeId }}</span>
                </div>
                <div class="detail-row" *ngIf="node.ipAddress">
                  <span class="label">IP:</span>
                  <span class="value">{{ node.ipAddress }}</span>
                </div>
                <div class="detail-row">
                  <span class="label">تعداد تشخیص:</span>
                  <span class="value">{{ node.totalDetections }}</span>
                </div>
                <div class="detail-row">
                  <span class="label">آخرین ارتباط:</span>
                  <span class="value">{{ formatDate(node.lastHeartbeat) }}</span>
                </div>
                <div class="detail-row" *ngIf="node.lastDetectionTime">
                  <span class="label">آخرین تشخیص:</span>
                  <span class="value">{{ formatDate(node.lastDetectionTime) }}</span>
                </div>
                <div class="detail-row">
                  <span class="label">ثبت شده در:</span>
                  <span class="value">{{ formatDate(node.registeredAt) }}</span>
                </div>
              </div>
            </div>
          </div>
          <div *ngIf="nodes.length === 0" class="no-data">
            هیچ نودی ثبت نشده است
          </div>
        </div>
      </div>
    </div>
  `,
    styles: [`
    .nodes-page {
      padding: 20px 0;
    }

    .page-title {
      font-size: 28px;
      font-weight: 700;
      margin-bottom: 30px;
      color: #323130;
    }

    .stats-row {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
      gap: 20px;
      margin-bottom: 30px;
    }

    .stat-box {
      background: white;
      border-radius: 8px;
      padding: 24px;
      text-align: center;
      box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
    }

    .stat-box.online {
      background: linear-gradient(135deg, #51cf66 0%, #37b24d 100%);
      color: white;
    }

    .stat-box.offline {
      background: linear-gradient(135deg, #ff6b6b 0%, #ee5a6f 100%);
      color: white;
    }

    .stat-box.total {
      background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
      color: white;
    }

    .stat-number {
      font-size: 36px;
      font-weight: 700;
      margin-bottom: 8px;
    }

    .stat-text {
      font-size: 14px;
      opacity: 0.9;
    }

    .nodes-grid {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(350px, 1fr));
      gap: 20px;
    }

    .node-card {
      background: white;
      border: 2px solid #e1e1e1;
      border-radius: 8px;
      padding: 20px;
      transition: all 0.3s ease;
    }

    .node-card.online {
      border-color: #51cf66;
    }

    .node-card:hover {
      box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
    }

    .node-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 16px;
      padding-bottom: 12px;
      border-bottom: 1px solid #e1e1e1;
    }

    .node-header h4 {
      margin: 0;
      font-size: 18px;
      font-weight: 700;
      color: #323130;
    }

    .status-badge {
      padding: 4px 12px;
      border-radius: 12px;
      font-size: 12px;
      font-weight: 600;
      background: #fde7e9;
      color: #d13438;
    }

    .status-badge.online {
      background: #dff6dd;
      color: #107c10;
    }

    .node-details {
      display: flex;
      flex-direction: column;
      gap: 10px;
    }

    .detail-row {
      display: flex;
      justify-content: space-between;
      font-size: 14px;
    }

    .detail-row .label {
      color: #666;
      font-weight: 600;
    }

    .detail-row .value {
      color: #323130;
      text-align: left;
    }

    .no-data {
      text-align: center;
      padding: 40px;
      color: #666;
    }

    @media (max-width: 768px) {
      .nodes-grid {
        grid-template-columns: 1fr;
      }
    }
  `]
})
export class NodesComponent implements OnInit {
    nodes: NodeInfo[] = [];
    loading = true;

    get onlineNodesCount(): number {
        return this.nodes.filter(n => n.isOnline).length;
    }

    get offlineNodesCount(): number {
        return this.nodes.filter(n => !n.isOnline).length;
    }

    constructor(private apiService: ApiService) { }

    ngOnInit() {
        this.loadNodes();

        // به‌روزرسانی خودکار هر 10 ثانیه
        setInterval(() => {
            this.loadNodes();
        }, 10000);
    }

    loadNodes() {
        this.apiService.getAllNodes().subscribe({
            next: (response) => {
                if (response.success) {
                    this.nodes = response.data;
                }
                this.loading = false;
            },
            error: (error) => {
                console.error('خطا در دریافت نودها:', error);
                this.loading = false;
            }
        });
    }

    formatDate(date: Date): string {
        const d = new Date(date);
        return d.toLocaleString('fa-IR');
    }
}


