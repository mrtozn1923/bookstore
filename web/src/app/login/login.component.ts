import { Component } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';

@Component({
    selector: 'app-login',
    templateUrl: './login.component.html',
    styleUrls: ['./login.component.css'],
    standalone: true
})
export class LoginComponent {

  constructor(private route: ActivatedRoute, private router: Router) { }

  signIn(): void {
    localStorage.setItem('token', 'demo-token');
    const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl') ?? '/home';
    this.router.navigateByUrl(returnUrl);
  }
}
