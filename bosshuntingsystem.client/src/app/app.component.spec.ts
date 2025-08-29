import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { AppComponent } from './app.component';

describe('AppComponent', () => {
  let component: AppComponent;
  let fixture: ComponentFixture<AppComponent>;
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [AppComponent],
      imports: [HttpClientTestingModule]
    }).compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(AppComponent);
    component = fixture.componentInstance;
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should create the app', () => {
    expect(component).toBeTruthy();
  });

  it('should load bosses on initialization', () => {
    const mockBosses = [
      { id: 1, name: 'Test Boss', respawnHours: 24, lastKilledAt: '2023-01-01T00:00:00Z', nextRespawnAt: '2023-01-02T00:00:00Z', isAvailable: true }
    ];

    component.ngOnInit();

    const req = httpMock.expectOne('/api/bosses');
    expect(req.request.method).toEqual('GET');
    req.flush(mockBosses);

    expect(component.bosses.length).toBe(1);
    expect(component.bosses[0].name).toBe('Test Boss');
  });
});