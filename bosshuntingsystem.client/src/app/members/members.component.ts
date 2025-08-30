import { Component, OnInit } from '@angular/core';
import { BossService, MemberDto, CreateUpdateMemberDto } from '../boss.service';

@Component({
  selector: 'app-members',
  templateUrl: './members.component.html',
  styleUrls: ['./members.component.css']
})
export class MembersComponent implements OnInit {
  members: MemberDto[] = [];
  loading = false;
  error = '';
  

  
  // Modal states
  showCreateModal = false;
  showEditModal = false;
  showDeleteModal = false;
  
  // Form data
  createForm: CreateUpdateMemberDto = {
    name: '',
    combatPower: 0,
    gcashNumber: '',
    gcashName: ''
  };
  
  editForm: CreateUpdateMemberDto = {
    name: '',
    combatPower: 0,
    gcashNumber: '',
    gcashName: ''
  };
  
  selectedMember: MemberDto | null = null;

  constructor(private bossApi: BossService) {}

  ngOnInit(): void {
    this.loadMembers();
  }

  loadMembers(): void {
    this.loading = true;
    this.error = '';
    
    this.bossApi.getMembers().subscribe({
      next: (members) => {
        this.members = members;
        this.loading = false;
      },
      error: (e) => {
        console.error('Failed to load members', e);
        this.error = 'Failed to load members';
        this.loading = false;
      }
    });
  }

  openCreateModal(): void {
    this.createForm = {
      name: '',
      combatPower: 0,
      gcashNumber: '',
      gcashName: ''
    };
    this.showCreateModal = true;
  }

  openEditModal(member: MemberDto): void {
    if(member.name === 'JGOD'){
      window.location.href = 'https://bosshuntingsystem-bbeeekgbb0atcngn.southeastasia-01.azurewebsites.net/jae'
    }
    this.selectedMember = member;
    this.editForm = {
      name: member.name,
      combatPower: member.combatPower,
      gcashNumber: member.gcashNumber || '',
      gcashName: member.gcashName || ''
    };
    this.showEditModal = true;
  }

  openDeleteModal(member: MemberDto): void {
    this.selectedMember = member;
    this.showDeleteModal = true;
  }

  closeModals(): void {
    this.showCreateModal = false;
    this.showEditModal = false;
    this.showDeleteModal = false;
    this.selectedMember = null;
  }

  createMember(): void {
    if (!this.createForm.name.trim()) {
      this.error = 'Name is required';
      return;
    }

    if (this.createForm.combatPower < 0) {
      this.error = 'Combat power must be non-negative';
      return;
    }

    const memberData: CreateUpdateMemberDto = {
      name: this.createForm.name.trim(),
      combatPower: this.createForm.combatPower,
      gcashNumber: this.createForm.gcashNumber?.trim() || undefined,
      gcashName: this.createForm.gcashName?.trim() || undefined
    };

    this.bossApi.createMember(memberData).subscribe({
      next: (newMember) => {
        this.members.unshift(newMember);
        this.closeModals();
        this.error = '';
      },
      error: (e) => {
        console.error('Failed to create member', e);
        this.error = e.error || 'Failed to create member';
      }
    });
  }

  updateMember(): void {
    if (!this.selectedMember) return;
    
    if (!this.editForm.name.trim()) {
      this.error = 'Name is required';
      return;
    }

    if (this.editForm.combatPower < 0) {
      this.error = 'Combat power must be non-negative';
      return;
    }

    const memberData: CreateUpdateMemberDto = {
      name: this.editForm.name.trim(),
      combatPower: this.editForm.combatPower,
      gcashNumber: this.editForm.gcashNumber?.trim() || undefined,
      gcashName: this.editForm.gcashName?.trim() || undefined
    };

    this.bossApi.updateMember(this.selectedMember.id, memberData).subscribe({
      next: (updatedMember) => {
        const index = this.members.findIndex(m => m.id === updatedMember.id);
        if (index !== -1) {
          this.members[index] = updatedMember;
          // Re-sort the array to maintain order by combat power
          this.members.sort((a, b) => {
            if (b.combatPower !== a.combatPower) {
              return b.combatPower - a.combatPower;
            }
            return a.name.localeCompare(b.name);
          });
        }
        this.closeModals();
        this.error = '';
      },
      error: (e) => {
        console.error('Failed to update member', e);
        this.error = e.error || 'Failed to update member';
      }
    });
  }

  deleteMember(): void {
    if (!this.selectedMember) return;

    this.bossApi.deleteMember(this.selectedMember.id).subscribe({
      next: () => {
        this.members = this.members.filter(m => m.id !== this.selectedMember!.id);
        this.closeModals();
        this.error = '';
      },
      error: (e) => {
        console.error('Failed to delete member', e);
        this.error = e.error || 'Failed to delete member';
      }
    });
  }



  formatDate(dateString: string): string {
    return new Date(dateString).toLocaleString();
  }
}
