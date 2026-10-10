import { ChangeDetectionStrategy, Component, input, output, signal } from '@angular/core';

/**
 * Vertical drag handle between two panes. Emits the new width of the pane on its left.
 * Supports mouse/touch/pen dragging, arrow keys (Shift = larger steps), Home/End and double-click to reset.
 */
@Component({
  selector: 'app-split-resizer',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    role: 'separator',
    tabindex: '0',
    'aria-orientation': 'vertical',
    'aria-label': 'Resize job details panel',
    '[attr.aria-valuenow]': 'width()',
    '[attr.aria-valuemin]': 'min()',
    '[attr.aria-valuemax]': 'max()',
    '[class.dragging]': 'dragging()',
    '(pointerdown)': 'onPointerDown($event)',
    '(keydown)': 'onKeyDown($event)',
    '(dblclick)': 'reset.emit()',
  },
  template: '<span class="grip" aria-hidden="true"></span>',
  styles: `
    :host {
      flex: 0 0 8px;
      box-sizing: border-box;
      position: relative;
      cursor: col-resize;
      touch-action: none;
      outline: none;
      background: var(--mat-sys-outline-variant);
      background-clip: content-box;
      padding: 0 3.5px;
      transition: background-color 120ms;
    }
    :host:hover,
    :host:focus-visible,
    :host.dragging {
      background-color: var(--mat-sys-primary);
      padding: 0 2.5px;
    }
    .grip {
      position: absolute;
      top: 50%;
      left: 50%;
      width: 6px;
      height: 40px;
      transform: translate(-50%, -50%);
      border-radius: 3px;
      background: var(--mat-sys-outline);
    }
    :host:hover .grip,
    :host.dragging .grip {
      background: var(--mat-sys-primary);
    }
  `,
})
export class SplitResizer {
  /** Current width (px) of the pane being resized. */
  readonly width = input.required<number>();
  readonly min = input(320);
  readonly max = input(1200);

  readonly widthChange = output<number>();
  readonly reset = output<void>();

  protected readonly dragging = signal(false);

  protected onPointerDown(event: PointerEvent): void {
    if (event.button !== 0) {
      return;
    }
    event.preventDefault();
    const handle = event.currentTarget as HTMLElement;
    handle.setPointerCapture(event.pointerId);
    handle.focus();

    const startX = event.clientX;
    const startWidth = this.width();
    this.dragging.set(true);
    document.body.style.userSelect = 'none';

    const move = (e: PointerEvent) => this.emit(startWidth + (e.clientX - startX));
    const end = () => {
      this.dragging.set(false);
      document.body.style.userSelect = '';
      handle.removeEventListener('pointermove', move);
      handle.removeEventListener('pointerup', end);
      handle.removeEventListener('pointercancel', end);
    };
    handle.addEventListener('pointermove', move);
    handle.addEventListener('pointerup', end);
    handle.addEventListener('pointercancel', end);
  }

  protected onKeyDown(event: KeyboardEvent): void {
    const step = event.shiftKey ? 80 : 20;
    const target =
      event.key === 'ArrowLeft'
        ? this.width() - step
        : event.key === 'ArrowRight'
          ? this.width() + step
          : event.key === 'Home'
            ? this.min()
            : event.key === 'End'
              ? this.max()
              : null;
    if (target !== null) {
      event.preventDefault();
      this.emit(target);
    }
  }

  private emit(width: number): void {
    this.widthChange.emit(Math.round(Math.min(this.max(), Math.max(this.min(), width))));
  }
}
