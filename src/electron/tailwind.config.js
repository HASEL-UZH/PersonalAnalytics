/** @type {import('tailwindcss').Config} */

module.exports = {
  content: ['./index.html', './src/**/*.{vue,js,ts,jsx,tsx}', './external/dyn/**/*.{vue,js,ts,jsx,tsx}'],
  plugins: [require('@tailwindcss/typography'), require('daisyui')],
  theme: {
    extend: {}
  },
  safelist: ['bg-neutral-700']
};
