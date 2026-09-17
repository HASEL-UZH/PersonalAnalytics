export interface TooltipAnchor {
  x: number;
  top: number;
  bottom: number;
}

interface TooltipSize {
  width: number;
  height: number;
}

/**
 * Positions an already-populated tooltip around its anchor while keeping it inside the visible
 * renderer viewport. The tooltip is shown below the anchor when there is not enough room above it.
 */
export function positionTooltipWithinViewport(
  tooltip: HTMLElement,
  anchor: TooltipAnchor,
  viewportPadding = 8,
  tooltipGap = 10
): void {
  const tooltipRect = tooltip.getBoundingClientRect();
  const viewport = {
    width: window.innerWidth,
    height: window.innerHeight
  };
  const left = getClampedHorizontalPosition(anchor.x, tooltipRect, viewport, viewportPadding);
  const top = getClampedVerticalPosition(
    anchor,
    tooltipRect,
    viewport,
    viewportPadding,
    tooltipGap
  );

  tooltip.style.position = 'fixed';
  tooltip.style.left = `${left}px`;
  tooltip.style.top = `${top}px`;
  tooltip.style.transform = 'none';
}

function getClampedHorizontalPosition(
  anchorX: number,
  tooltip: TooltipSize,
  viewport: TooltipSize,
  viewportPadding: number
): number {
  const maxLeft = Math.max(viewportPadding, viewport.width - tooltip.width - viewportPadding);
  return clamp(anchorX - tooltip.width / 2, viewportPadding, maxLeft);
}

function getClampedVerticalPosition(
  anchor: TooltipAnchor,
  tooltip: TooltipSize,
  viewport: TooltipSize,
  viewportPadding: number,
  tooltipGap: number
): number {
  const topAboveAnchor = anchor.top - tooltip.height - tooltipGap;
  if (topAboveAnchor >= viewportPadding) {
    return topAboveAnchor;
  }

  const topBelowAnchor = anchor.bottom + tooltipGap;
  const maxTop = Math.max(viewportPadding, viewport.height - tooltip.height - viewportPadding);
  return clamp(topBelowAnchor, viewportPadding, maxTop);
}

function clamp(value: number, min: number, max: number): number {
  return Math.min(Math.max(value, min), max);
}
