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
  selectedWeekOffset: number = 0; // 0 = current week, 1 = last week, 2 = two weeks ago, etc.
  filterStartDate: string = '';
  filterEndDate: string = '';
  weekOptions: { value: number, label: string, dateRange: string }[] = [];
  private isLoading = false; // Prevent multiple simultaneous API calls

  // Dividend calculation properties
  showDividendsCalculator = false;
  totalSales: number = 0;
  startDate: string = '';
  endDate: string = '';
  dividendsResult: DividendsCalculationResult | null = null;
  calculatingDividends = false;

  constructor(private bossService: BossService, private dateUtils: DateUtilsService) {}

  ngOnInit(): void {
    // Generate week options once
    this.generateWeekOptions();

    // Set default date range to current week in PHT (GMT+8)
    const now = new Date();
    const utc = now.getTime() + (now.getTimezoneOffset() * 60000);
    const phtToday = new Date(utc + (8 * 3600000)); // GMT+8

    const dayOfWeek = phtToday.getDay();
    const daysFromMonday = dayOfWeek === 0 ? 6 : dayOfWeek - 1; // Sunday = 6 days from Monday, otherwise dayOfWeek - 1

    const startOfWeek = new Date(phtToday);
    startOfWeek.setDate(phtToday.getDate() - daysFromMonday);
    startOfWeek.setHours(0, 0, 0, 0);

    const endOfWeek = new Date(startOfWeek);
    endOfWeek.setDate(startOfWeek.getDate() + 6);
    endOfWeek.setHours(23, 59, 59, 999);

    this.startDate = startOfWeek.toISOString().split('T')[0];
    this.endDate = endOfWeek.toISOString().split('T')[0];

    // Initialize filter dates
    this.setFilterPeriod('all');
  }

  loadMemberPoints(): void {
    // Prevent multiple simultaneous API calls
    if (this.isLoading) {
      console.log('[PointsComponent] Already loading, skipping duplicate request');
      return;
    }

    this.isLoading = true;
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
      selectedWeekOffset: this.selectedWeekOffset,
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
        this.isLoading = false;

        // Clear previous dividend results since data has changed
        this.dividendsResult = null;
      },
      error: (error) => {
        console.error('Error loading member points:', error);
        this.memberPoints = [];
        this.filteredMemberPoints = [];
        this.loading = false;
        this.isLoading = false;

        // Clear dividend results on error
        this.dividendsResult = null;
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

    if (this.memberPoints.length === 0) {
      alert('No member points data available. Please wait for data to load.');
      return;
    }

    this.calculatingDividends = true;

    // Use the exact same member points data that's currently displayed
    const totalPoints = this.memberPoints.reduce((total, member) => total + member.points, 0);
    const pointsPerPeso = totalPoints > 0 ? this.totalSales / totalPoints : 0;

    console.log('[PointsComponent] Calculating dividends with displayed data:', {
      filterPeriod: this.filterPeriod,
      selectedWeekOffset: this.selectedWeekOffset,
      totalSales: this.totalSales,
      totalPoints,
      pointsPerPeso,
      membersCount: this.memberPoints.length
    });

    // Calculate dividends using the same data that's displayed
    const memberDividends = this.memberPoints.map(member => ({
      memberName: member.memberName,
      points: member.points,
      dividend: member.points * pointsPerPeso
    }));

    // Create result object that matches API response format
    this.dividendsResult = {
      totalSales: this.totalSales,
      totalPoints: totalPoints,
      periodStart: this.filterPeriod !== 'all' && this.filterStartDate ? this.filterStartDate : undefined,
      periodEnd: this.filterPeriod !== 'all' && this.filterEndDate ? this.filterEndDate : undefined,
      memberDividends: memberDividends,
      calculatedAt: new Date().toISOString()
    };

    this.calculatingDividends = false;

    console.log('[PointsComponent] Dividends calculated:', this.dividendsResult);
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
        this.applyDateFilter();
        break;
      case 'week':
        // Week filter is handled by onWeekSelectionChange, don't apply here
        this.setWeekFilter(this.selectedWeekOffset);
        return; // Don't apply filter here, will be done by week selection
      case 'month':
        const startOfMonth = new Date(phtToday);
        startOfMonth.setDate(1);
        startOfMonth.setHours(0, 0, 0, 0);

        const endOfMonth = new Date(phtToday.getFullYear(), phtToday.getMonth() + 1, 0);
        endOfMonth.setHours(23, 59, 59, 999);

        this.filterStartDate = startOfMonth.toISOString().split('T')[0];
        this.filterEndDate = endOfMonth.toISOString().split('T')[0];
        this.applyDateFilter();
        break;
      case 'custom':
        // Keep existing dates or set to current week as default
        if (!this.filterStartDate || !this.filterEndDate) {
          const dayOfWeekDefault = phtToday.getDay();
          const daysFromMondayDefault = dayOfWeekDefault === 0 ? 6 : dayOfWeekDefault - 1;

          const defaultStart = new Date(phtToday);
          defaultStart.setDate(phtToday.getDate() - daysFromMondayDefault);
          defaultStart.setHours(0, 0, 0, 0);

          const defaultEnd = new Date(defaultStart);
          defaultEnd.setDate(defaultStart.getDate() + 6);
          defaultEnd.setHours(23, 59, 59, 999);

          this.filterStartDate = defaultStart.toISOString().split('T')[0];
          this.filterEndDate = defaultEnd.toISOString().split('T')[0];
        }
        this.applyDateFilter();
        break;
    }
  }

  applyDateFilter(): void {
    this.loadMemberPoints();
  }

  setWeekFilter(weeksAgo: number): void {
    this.selectedWeekOffset = weeksAgo;

    // Get current date in PHT (GMT+8) timezone
    const now = new Date();
    const utc = now.getTime() + (now.getTimezoneOffset() * 60000);
    const phtToday = new Date(utc + (8 * 3600000)); // GMT+8

    const dayOfWeek = phtToday.getDay();
    // Calculate days to go back to reach Monday
    // Sunday (0) = go back 6 days to reach Monday of current week
    // Monday (1) = go back 0 days (already Monday)
    // Tuesday (2) = go back 1 day, etc.
    const daysFromMonday = dayOfWeek === 0 ? 6 : dayOfWeek - 1;

    // Calculate the start of the selected week using regular Date operations
    const startOfSelectedWeek = new Date(phtToday);
    startOfSelectedWeek.setDate(phtToday.getDate() - daysFromMonday - (weeksAgo * 7));
    startOfSelectedWeek.setHours(0, 0, 0, 0); // Start of day

    const endOfSelectedWeek = new Date(startOfSelectedWeek);
    endOfSelectedWeek.setDate(startOfSelectedWeek.getDate() + 6);
    endOfSelectedWeek.setHours(23, 59, 59, 999); // End of day

    console.log('[Points] Week calculation:', {
      phtToday: phtToday.toISOString(),
      currentYear: phtToday.getFullYear(),
      dayOfWeek,
      daysFromMonday,
      weeksAgo,
      startOfSelectedWeek: startOfSelectedWeek.toISOString(),
      endOfSelectedWeek: endOfSelectedWeek.toISOString(),
      startDateString: startOfSelectedWeek.toISOString().split('T')[0],
      endDateString: endOfSelectedWeek.toISOString().split('T')[0]
    });

    this.filterStartDate = startOfSelectedWeek.toISOString().split('T')[0];
    this.filterEndDate = endOfSelectedWeek.toISOString().split('T')[0];
  }

  generateWeekOptions(): void {
    this.weekOptions = [];
    const now = new Date();
    const utc = now.getTime() + (now.getTimezoneOffset() * 60000);
    const phtToday = new Date(utc + (8 * 3600000)); // GMT+8

    for (let i = 0; i < 8; i++) { // Show current week + 7 previous weeks
      const dayOfWeek = phtToday.getDay();
      const daysFromMonday = dayOfWeek === 0 ? 6 : dayOfWeek - 1;

      // Calculate the start of the week using regular Date operations
      const startOfWeek = new Date(phtToday);
      startOfWeek.setDate(phtToday.getDate() - daysFromMonday - (i * 7));
      startOfWeek.setHours(0, 0, 0, 0);

      const endOfWeek = new Date(startOfWeek);
      endOfWeek.setDate(startOfWeek.getDate() + 6);

      let label = '';
      if (i === 0) {
        label = 'This Week';
      } else if (i === 1) {
        label = 'Last Week';
      } else {
        label = `${i} Weeks Ago`;
      }

      const dateRange = `${startOfWeek.toLocaleDateString('en-US', { month: 'short', day: 'numeric' })} - ${endOfWeek.toLocaleDateString('en-US', { month: 'short', day: 'numeric' })}`;

      this.weekOptions.push({
        value: i,
        label: label,
        dateRange: dateRange
      });
    }
  }

  getWeekOptions(): { value: number, label: string, dateRange: string }[] {
    return this.weekOptions;
  }

  onWeekSelectionChange(weeksAgo: number): void {
    console.log('[PointsComponent] Week selection changed to:', weeksAgo);
    this.filterPeriod = 'week';
    this.setWeekFilter(weeksAgo);
    this.applyDateFilter();
  }

  getFilterSummary(): string {
    switch (this.filterPeriod) {
      case 'all':
        return 'All Time';
      case 'week':
        if (this.selectedWeekOffset === 0) {
          return 'This Week';
        } else if (this.selectedWeekOffset === 1) {
          return 'Last Week';
        } else {
          return `${this.selectedWeekOffset} Weeks Ago`;
        }
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