import { AnimationCallbackEvent, Component, inject } from '@angular/core';
import { ToastService } from '../toast.service';

@Component({
  selector: 'app-toast-host',
  styleUrl: './toast-host.component.css',
  template: `
    @for (toast of toasts.items(); track toast.id) {
      <p class="toast" role="status" animate.enter="toast-enter" (animate.leave)="slideOut($event)">
        {{ toast.text }}
      </p>
    }
  `
})
export class ToastHostComponent {
  protected readonly toasts = inject(ToastService);

  protected slideOut(event: AnimationCallbackEvent): void {
    if (matchMedia('(prefers-reduced-motion: reduce)').matches) {
      event.animationComplete();
      return;
    }
    const animation = event.target.animate(
      [{ opacity: 1, transform: 'none' }, { opacity: 0, transform: 'translateY(16px)' }],
      { duration: 200, easing: 'ease-in' }
    );
    // Angular removes the element only after this call
    animation.finished.then(() => event.animationComplete());
  }
}
