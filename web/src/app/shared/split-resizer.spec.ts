import { ComponentFixture, TestBed } from '@angular/core/testing';
import { SplitResizer } from './split-resizer';

describe('SplitResizer', () => {
  let fixture: ComponentFixture<SplitResizer>;
  let emitted: number[];
  let resets: number;

  beforeEach(async () => {
    fixture = TestBed.createComponent(SplitResizer);
    fixture.componentRef.setInput('width', 500);
    fixture.componentRef.setInput('min', 340);
    fixture.componentRef.setInput('max', 800);
    emitted = [];
    resets = 0;
    fixture.componentInstance.widthChange.subscribe((w) => emitted.push(w));
    fixture.componentInstance.reset.subscribe(() => resets++);
    await fixture.whenStable();
  });

  const key = (k: string, shiftKey = false) =>
    fixture.nativeElement.dispatchEvent(new KeyboardEvent('keydown', { key: k, shiftKey }));

  it('is an accessible vertical separator', () => {
    const el: HTMLElement = fixture.nativeElement;
    expect(el.getAttribute('role')).toBe('separator');
    expect(el.getAttribute('aria-valuenow')).toBe('500');
    expect(el.getAttribute('tabindex')).toBe('0');
  });

  it('resizes with arrow keys, larger steps with Shift', () => {
    key('ArrowRight');
    key('ArrowLeft', true);
    expect(emitted).toEqual([520, 420]);
  });

  it('clamps to min and max', () => {
    key('Home');
    key('End');
    fixture.componentRef.setInput('width', 790);
    key('ArrowRight', true);
    expect(emitted).toEqual([340, 800, 800]);
  });

  it('requests a reset on double-click', () => {
    fixture.nativeElement.dispatchEvent(new MouseEvent('dblclick'));
    expect(resets).toBe(1);
  });
});
