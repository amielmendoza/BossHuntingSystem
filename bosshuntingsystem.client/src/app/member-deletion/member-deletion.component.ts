import { Component, OnInit } from '@angular/core';
import { BossService, MemberWithBossHuntsDto } from '../boss.service';

@Component({
  selector: 'app-member-deletion',
  templateUrl: './member-deletion.component.html',
  styleUrls: ['./member-deletion.component.css']
})
export class MemberDeletionComponent implements OnInit {
  members: MemberWithBossHuntsDto[] = [];
  loading = false;
  error = '';
  successMessage = '';

  selectedMemberName: string | null = null;
  showConfirmationModal = false;
  deleting = false;

  constructor(private bossService: BossService) {}

  ngOnInit(): void {
    this.loadMembersWithBossHunts();
  }

  loadMembersWithBossHunts(): void {
    this.loading = true;
    this.error = '';
    this.successMessage = '';

    this.bossService.getMembersWithBossHunts().subscribe({
      next: (members) => {
        this.members = members;
        this.loading = false;
      },
      error: (e) => {
        console.error('Failed to load members with boss hunts', e);
        this.error = 'Failed to load members with boss hunts';
        this.loading = false;
      }
    });
  }

  onMemberSelected(): void {
    this.error = '';
    this.successMessage = '';
  }

  openConfirmationModal(): void {
    if (!this.selectedMemberName) {
      this.error = 'Please select a member first';
      return;
    }
    this.showConfirmationModal = true;
  }

  closeConfirmationModal(): void {
    this.showConfirmationModal = false;
  }

  getSelectedMember(): MemberWithBossHuntsDto | null {
    if (!this.selectedMemberName) return null;
    return this.members.find(m => m.name === this.selectedMemberName) || null;
  }

  deleteMemberRecords(): void {
    if (!this.selectedMemberName) return;

    this.deleting = true;
    this.error = '';

    this.bossService.deleteMemberRecords(this.selectedMemberName).subscribe({
      next: (result) => {
        this.successMessage = result.message;
        this.closeConfirmationModal();
        this.selectedMemberName = null;
        this.deleting = false;
        // Refresh the list to update boss hunt counts
        this.loadMembersWithBossHunts();
      },
      error: (e) => {
        console.error('Failed to delete member records', e);
        this.error = e.error?.message || 'Failed to delete member records';
        this.deleting = false;
      }
    });
  }
}