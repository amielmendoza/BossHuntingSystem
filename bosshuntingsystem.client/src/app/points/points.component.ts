import { Component, OnInit } from '@angular/core';
import { BossService, MemberPointsDto, DividendsCalculationRequest, DividendsCalculationResult } from '../boss.service';
import { DateUtilsService } from '../utils/date-utils.service';

@Component({
  selector: 'app-points',
  templateUrl: './points.component.html',
  styleUrls: ['./points.component.css']
})
export class PointsComponent implements OnInit {
  memberPoints: MemberPointsDto[] = [];
  filteredMemberPoints: MemberPointsDto[] = [];
  loading = true;

  // Date filter properties
  filterPeriod: 'all' | 'week' | 'month' | 'custom' = 'all';
  filterStartDate: string = '';
  filterEndDate: string = '';

  // Dividend calculation properties
  showDividendsCalculator = false;
  totalSales: number = 0;
  startDate: string = '';
  endDate: string = '';
  dividendsResult: DividendsCalculationResult | null = null;
  calculatingDividends = false;

  constructor(private bossService: BossService, private dateUtils: DateUtilsService) {}

  ngOnInit(): void {
    this.loadMemberPoints();
    // Set default date range to current week in PHT (GMT+8)
    const now = new Date();
    const utc = now.getTime() + (now.getTimezoneOffset() * 60000);
    const phtToday = new Date(utc + (8 * 3600000)); // GMT+8

    const dayOfWeek = phtToday.getDay();
    const daysFromMonday = dayOfWeek === 0 ? 6 : dayOfWeek - 1; // Sunday = 6 days from Monday, otherwise dayOfWeek - 1
    const startOfWeek = new Date(Date.UTC(phtToday.getFullYear(), phtToday.getMonth(), phtToday.getDate() - daysFromMonday));
    const endOfWeek = new Date(Date.UTC(phtToday.getFullYear(), phtToday.getMonth(), phtToday.getDate() - daysFromMonday + 6));

    this.startDate = startOfWeek.toISOString().split('T')[0];
    this.endDate = endOfWeek.toISOString().split('T')[0];

    // Initialize filter dates
    this.setFilterPeriod('all');
  }

  loadMemberPoints(): void {
    this.loading = true;

    // Clear existing data to prevent stale data display
    this.memberPoints = [];
    this.filteredMemberPoints = [];

    // Get the current filter parameters for the API call
    let startDate: string | undefined;
    let endDate: string | undefined;

    if (this.filterPeriod !== 'all') {
      startDate = this.filterStartDate || undefined;
      endDate = this.filterEndDate || undefined;
    }

    console.log('[PointsComponent] Loading member points with filter:', {
      filterPeriod: this.filterPeriod,
      startDate,
      endDate,
      filterStartDate: this.filterStartDate,
      filterEndDate: this.filterEndDate
    });

    this.bossService.getMemberPointsWithDateFilter(startDate, endDate).subscribe({
      next: (points) => {
        console.log('[PointsComponent] Received points data:', points);
        this.memberPoints = points;
        this.filteredMemberPoints = points;
        this.loading = false;
      },
      error: (error) => {
        console.error('Error loading member points:', error);
        this.memberPoints = [];
        this.filteredMemberPoints = [];
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

  getTotalPoints(): number {
    return this.memberPoints.reduce((total, member) => total + member.points, 0);
  }

  getAveragePoints(): number {
    if (this.memberPoints.length === 0) return 0;
    return this.getTotalPoints() / this.memberPoints.length;
  }

  getHighestScore(): number {
    if (this.memberPoints.length === 0) return 0;
    return this.memberPoints[0]?.points || 0;
  }

  getPerformancePercentage(points: number): number {
    const highestScore = this.getHighestFilteredScore();
    if (highestScore === 0) return 0;
    return (points / highestScore) * 100;
  }

  // Date filter methods
  setFilterPeriod(period: 'all' | 'week' | 'month' | 'custom'): void {
    this.filterPeriod = period;

    // Get current date in PHT (GMT+8) timezone
    const now = new Date();
    const utc = now.getTime() + (now.getTimezoneOffset() * 60000);
    const phtToday = new Date(utc + (8 * 3600000)); // GMT+8

    switch (period) {
      case 'all':
        this.filterStartDate = '';
        this.filterEndDate = '';
        break;
      case 'week':
        const dayOfWeek = phtToday.getDay();
        // Calculate days to go back to reach Monday
        // Sunday (0) = go back 6 days to reach Monday of current week
        // Monday (1) = go back 0 days (already Monday)
        // Tuesday (2) = go back 1 day, etc.
        const daysFromMonday = dayOfWeek === 0 ? 6 : dayOfWeek - 1;
        const startOfWeek = new Date(Date.UTC(phtToday.getFullYear(), phtToday.getMonth(), phtToday.getDate() - daysFromMonday));
        const endOfWeek = new Date(Date.UTC(phtToday.getFullYear(), phtToday.getMonth(), phtToday.getDate() - daysFromMonday + 6));

        console.log('[Points] Week calculation:', {
          phtToday: phtToday.toISOString(),
          dayOfWeek,
          daysFromMonday,
          startOfWeek: startOfWeek.toISOString(),
          endOfWeek: endOfWeek.toISOString(),
          startDateString: startOfWeek.toISOString().split('T')[0],
          endDateString: endOfWeek.toISOString().split('T')[0]
        });

        this.filterStartDate = startOfWeek.toISOString().split('T')[0];
        this.filterEndDate = endOfWeek.toISOString().split('T')[0];
        break;
      case 'month':
        const startOfMonth = new Date(Date.UTC(phtToday.getFullYear(), phtToday.getMonth(), 1));
        const endOfMonth = new Date(Date.UTC(phtToday.getFullYear(), phtToday.getMonth() + 1, 0));
        this.filterStartDate = startOfMonth.toISOString().split('T')[0];
        this.filterEndDate = endOfMonth.toISOString().split('T')[0];
        break;
      case 'custom':
        // Keep existing dates or set to current week as default
        if (!this.filterStartDate || !this.filterEndDate) {
          const dayOfWeekDefault = phtToday.getDay();
          const daysFromMondayDefault = dayOfWeekDefault === 0 ? 6 : dayOfWeekDefault - 1;
          const defaultStart = new Date(Date.UTC(phtToday.getFullYear(), phtToday.getMonth(), phtToday.getDate() - daysFromMondayDefault));
          const defaultEnd = new Date(Date.UTC(phtToday.getFullYear(), phtToday.getMonth(), phtToday.getDate() - daysFromMondayDefault + 6));
          this.filterStartDate = defaultStart.toISOString().split('T')[0];
          this.filterEndDate = defaultEnd.toISOString().split('T')[0];
        }
        break;
    }

    this.applyDateFilter();
  }

  applyDateFilter(): void {
    this.loadMemberPoints();
  }

  getFilterSummary(): string {
    switch (this.filterPeriod) {
      case 'all':
        return 'All Time';
      case 'week':
        return 'This Week';
      case 'month':
        return 'This Month';
      case 'custom':
        if (this.filterStartDate && this.filterEndDate) {
          const start = new Date(this.filterStartDate).toLocaleDateString();
          const end = new Date(this.filterEndDate).toLocaleDateString();
          return `${start} - ${end}`;
        }
        return 'Custom Range';
      default:
        return 'All Time';
    }
  }

  getDisplayedMemberPoints(): MemberPointsDto[] {
    return this.filteredMemberPoints;
  }

  getTotalFilteredPoints(): number {
    return this.filteredMemberPoints.reduce((total, member) => total + member.points, 0);
  }

  getAverageFilteredPoints(): number {
    if (this.filteredMemberPoints.length === 0) return 0;
    return this.getTotalFilteredPoints() / this.filteredMemberPoints.length;
  }

  getHighestFilteredScore(): number {
    if (this.filteredMemberPoints.length === 0) return 0;
    return this.filteredMemberPoints[0]?.points || 0;
  }
}