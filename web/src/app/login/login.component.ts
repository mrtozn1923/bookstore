import { Component, ChangeDetectionStrategy } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';

@Component({
    selector: 'app-login',
    templateUrl: './login.component.html',
    changeDetection: ChangeDetectionStrategy.Eager,
    styleUrls: ['./login.component.css'],
})
export class LoginComponent {

  constructor(private route: ActivatedRoute, private router: Router) { }

  signIn(role = 'Customer'): void {
    localStorage.setItem('token', 'demo-token');
    localStorage.setItem('role', role);
    const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl') ?? '/home';
    this.router.navigateByUrl(returnUrl);
  }
}
