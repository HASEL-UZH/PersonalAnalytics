/** @type {import('tailwindcss').Config} */
import typography from '@tailwindcss/typography';
import colors from 'tailwindcss/colors';
import daisyui from 'daisyui';

export default {
  content: ['./index.html', './src/**/*.{vue,js,ts,jsx,tsx}'],
  plugins: [typography, daisyui],
  theme: {
    extend: {
      colors: {
        neutral: colors.neutral
      }
    }
  },
  daisyui: {
    themes: ['light', 'dark']
  },
  // needed for the dynamic classes
  safelist: [
    {
      pattern:
        /(bg|border|border-l|border-b|outline|text)-!?(amber|fuchsia|teal|rose|red|sky|orange|yellow|violet|pink|green|blue|neutral)-(300|400|500|600|800)/,
      variants: ['active', 'focus']
    }
  ]
};
