import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface VehicleDetection {
    id: string;
    nodeId: string;
    nodeName: string;
    plateNumber: string;
    speed: number;
    detectionTime: Date;
    imageFileName: string;
    confidence: number;
    vehicleType: number;
    vehicleColor?: string;
    isSpeedViolation: boolean;
    speedLimit: number;
}

export interface NodeInfo {
    nodeId: string;
    nodeName: string;
    lastHeartbeat: Date;
    isOnline: boolean;
    ipAddress?: string;
    version?: string;
    totalDetections: number;
    registeredAt: Date;
    lastDetectionTime?: Date;
}

export interface Statistics {
    totalDetections: number;
    todayDetections: number;
    speedViolations: number;
    activeNodes: number;
}

export interface ApiResponse<T> {
    success: boolean;
    message: string;
    data: T;
    errors?: string[];
    timestamp: Date;
}

@Injectable({
    providedIn: 'root'
})
export class ApiService {
    private apiUrl = environment.apiUrl;

    constructor(private http: HttpClient) { }

    // Vehicle Detections
    getRecentDetections(count: number = 100): Observable<ApiResponse<VehicleDetection[]>> {
        return this.http.get<ApiResponse<VehicleDetection[]>>(`${this.apiUrl}/vehicledetection/recent?count=${count}`);
    }

    getDetectionsByPlate(plateNumber: string): Observable<ApiResponse<VehicleDetection[]>> {
        return this.http.get<ApiResponse<VehicleDetection[]>>(`${this.apiUrl}/vehicledetection/by-plate/${plateNumber}`);
    }

    getStatistics(): Observable<ApiResponse<Statistics>> {
        return this.http.get<ApiResponse<Statistics>>(`${this.apiUrl}/vehicledetection/statistics`);
    }

    // Nodes
    getAllNodes(): Observable<ApiResponse<NodeInfo[]>> {
        return this.http.get<ApiResponse<NodeInfo[]>>(`${this.apiUrl}/node/list`);
    }

    getNode(nodeId: string): Observable<ApiResponse<NodeInfo>> {
        return this.http.get<ApiResponse<NodeInfo>>(`${this.apiUrl}/node/${nodeId}`);
    }
}


