/** @type {import('tailwindcss').Config} */
const withAlpha = (cssVar) => `rgb(var(${cssVar}) / <alpha-value>)`;

module.exports = {
  content: [
    "./Views/**/*.cshtml",
    "./Pages/**/*.cshtml",
    "./wwwroot/js/**/*.js"
  ],
  theme: {
    extend: {
      colors: {
        primary: withAlpha('--theme-primary-rgb'),
        primaryHover: withAlpha('--theme-primary-hover-rgb'),
        primaryLight: withAlpha('--theme-primary-light-rgb'),
        bgBase: withAlpha('--theme-bg-base-rgb'),
        bgSubtle: withAlpha('--theme-bg-subtle-rgb'),
        borderSubtle: withAlpha('--theme-border-subtle-rgb'),
        textMuted: withAlpha('--theme-text-muted-rgb'),
        accent: withAlpha('--theme-accent-rgb'),
        accentLight: withAlpha('--theme-accent-light-rgb'),
        accentRed: withAlpha('--theme-accent-red-rgb'),
        accentGreen: withAlpha('--theme-accent-green-rgb'),
        accentAmber: withAlpha('--theme-accent-amber-rgb'),
      },
      fontFamily: {
        sans: ['Cairo', 'sans-serif'],
      }
    },
  },
  plugins: [],
}
