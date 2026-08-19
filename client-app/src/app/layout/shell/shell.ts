import { Component, inject } from '@angular/core';
import { RouterLink, RouterOutlet } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { MatToolbarModule } from '@angular/material/toolbar';

import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [RouterLink, RouterOutlet, MatButtonModule, MatIconModule, MatMenuModule, MatToolbarModule],
  templateUrl: './shell.html',
  styleUrl: './shell.scss'
})
export class Shell {
  readonly authService = inject(AuthService);

  logout(): void {
    this.authService.logout();
  }
}
