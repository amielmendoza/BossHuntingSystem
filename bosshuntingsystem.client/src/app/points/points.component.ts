import { Component, OnInit } from '@angular/core';
import { BossService, MemberPointsDto, DividendsCalculationRequest, DividendsCalculationResult } from '../boss.service';

@Component({
  selector: 'app-points',
  templateUrl: './points.component.html',
  styleUrls: ['./points.component.css']
})
export class PointsComponent implements OnInit {
  memberPoints: MemberPointsDto[] = [];
  loading = true;
  
  // Dividend calculation properties
  showDividendsCalculator = false;
  totalSales: number = 0;
  startDate: string = '';
  endDate: string = '';
  dividendsResult: DividendsCalculationResult | null = null;
  calculatingDividends = false;

  constructor(private bossService: BossService) {}

  ngOnInit(): void {
    this.loadMemberPoints();
    // Set default date range to current week
    const today = new Date();
    const startOfWeek = new Date(today.getFullYear(), today.getMonth(), today.getDate() - today.getDay());
    const endOfWeek = new Date(today.getFullYear(), today.getMonth(), today.getDate() - today.getDay() + 6);
    
    this.startDate = startOfWeek.toISOString().split('T')[0];
    this.endDate = endOfWeek.toISOString().split('T')[0];
  }

  loadMemberPoints(): void {
    this.loading = true;
    this.bossService.getMemberPoints().subscribe({
      next: (points) => {
        this.memberPoints = points;
        this.loading = false;
      },
      error: (error) => {
        console.error('Error loading member points:', error);
        this.loading = false;
      }
    });
  }

  refreshPoints(): void {
    this.loadMemberPoints();
  }

  toggleDividendsCalculator(): void {
    this.showDividendsCalculator = !this.showDividendsCalculator;
    if (!this.showDividendsCalculator) {
      this.dividendsResult = null;
    }
  }

  calculateDividends(): void {
    if (!this.totalSales || this.totalSales <= 0) {
      alert('Please enter a valid total sales amount');
      return;
    }

    this.calculatingDividends = true;
    
    const request: DividendsCalculationRequest = {
      totalSales: this.totalSales,
      startDate: this.startDate || undefined,
      endDate: this.endDate || undefined
    };

    this.bossService.calculateDividends(request).subscribe({
      next: (result) => {
        this.dividendsResult = result;
        this.calculatingDividends = false;
      },
      error: (error) => {
        console.error('Error calculating dividends:', error);
        alert('Error calculating dividends: ' + (error.error?.message || error.message || 'Unknown error'));
        this.calculatingDividends = false;
      }
    });
  }

  getTotalDividends(): number {
    return this.dividendsResult?.memberDividends.reduce((total, member) => total + member.dividend, 0) || 0;
  }
}