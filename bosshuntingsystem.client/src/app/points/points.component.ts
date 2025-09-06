import { Component, OnInit } from '@angular/core';
import { BossService, MemberPointsDto } from '../boss.service';

@Component({
  selector: 'app-points',
  templateUrl: './points.component.html',
  styleUrls: ['./points.component.css']
})
export class PointsComponent implements OnInit {
  memberPoints: MemberPointsDto[] = [];
  loading = true;

  constructor(private bossService: BossService) {}

  ngOnInit(): void {
    this.loadMemberPoints();
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
}