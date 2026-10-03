// Chart colours, validated for colour-blind safety and contrast (dataviz palette check).
// Categorical: always assigned in this order, never cycled; at most 5 series per chart.
export const SERIES_COLORS = ['#2a78d6', '#eb6834', '#1baf7a', '#eda100', '#e87ba4'];

// Sequential (heatmap): one hue, light to dark. "None" uses a neutral grey so empty cells read as nothing.
export const SEQUENTIAL_BLUES = ['#cde2fb', '#9ec5f4', '#6da7ec', '#3987e5', '#256abf', '#184f95'];
export const EMPTY_CELL = '#f0efec';

export const CHART_TEXT = '#52514e';
export const CHART_GRID = '#e6e5e1';
export const CHART_SURFACE = '#ffffff';
export const CHART_FONT = 'Roboto, "Helvetica Neue", sans-serif';
