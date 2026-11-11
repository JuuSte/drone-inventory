import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Routes, provideRouter, Router } from '@angular/router';
import { App } from './app';

@Component({ selector: 'app-test-page', template: '<h1>Page</h1>' })
class TestPage {}

/** Stand-ins for the real pages: this spec only covers the layout shell. */
const testRoutes: Routes = [
  { path: 'drones', component: TestPage },
  { path: 'student-groups', component: TestPage },
  { path: 'loans', component: TestPage },
  { path: 'history', component: TestPage }
];

function navigationLinks(fixture: { nativeElement: HTMLElement }): HTMLAnchorElement[] {
  return Array.from(
    fixture.nativeElement.querySelectorAll<HTMLAnchorElement>('nav.main-nav a')
  );
}

describe('App', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [provideRouter(testRoutes)]
    }).compileComponents();
  });

  it('should create the app', () => {
    const fixture = TestBed.createComponent(App);
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('should render navigation for Drones, Student Groups, Loans and History', async () => {
    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();
    const links = navigationLinks(fixture as never).map((link) => link.textContent?.trim());

    expect(links).toEqual(['Drones', 'Student Groups', 'Loans', 'History']);
  });

  it('should mark the current page in the navigation', async () => {
    const fixture = TestBed.createComponent(App);
    const router = TestBed.inject(Router);

    await router.navigateByUrl('/loans');
    await fixture.whenStable();
    fixture.detectChanges();

    const links = navigationLinks(fixture as never);
    const current = links.filter((link) => link.getAttribute('aria-current') === 'page');
    const active = links.filter((link) => link.classList.contains('active'));

    expect(current.map((link) => link.textContent?.trim())).toEqual(['Loans']);
    expect(active.map((link) => link.textContent?.trim())).toEqual(['Loans']);
    expect(
      links
        .filter((link) => link.getAttribute('aria-current') !== 'page')
        .map((link) => link.textContent?.trim())
    ).toEqual(['Drones', 'Student Groups', 'History']);
  });
});
