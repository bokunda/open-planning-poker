/** @type {import('tailwindcss').Config} */
// Colors and fonts match the app (frontend/open-planning-poker-web-app/src/styles/_variables.scss):
// the brand violet and violet-tinted neutrals, Bricolage Grotesque for headings and Figtree for text.
const violet = {
  50: '#f4f1fe', 100: '#ebe4fd', 200: '#d9cbfb', 300: '#bba3f7', 400: '#9a76f1',
  500: '#7c4fec', 600: '#6a3ae8', 700: '#5a2bd0', 800: '#4a24a8', 900: '#3c1f86', 950: '#241455',
};
module.exports = {
  content: ['index.html', 'changelog.html', '404.html', 'pages/*.html', 'partials/*.html', 'i18n/*/*.html', 'generate-changelog.js'],
  theme: {
    extend: {
      colors: {
        primary: violet,
        purple: violet,
        gray: {
          50: '#f7f6fb', 100: '#efedf6', 200: '#e2dfec', 300: '#c9c5d8', 400: '#9a95b0',
          500: '#6b6687', 600: '#524d6b', 700: '#3a3552', 800: '#1d1a30', 900: '#0e0c19', 950: '#08070f',
        },
      },
      fontFamily: {
        sans: ['Figtree', 'Segoe UI', 'system-ui', 'sans-serif'],
        display: ['"Bricolage Grotesque"', 'Segoe UI', 'system-ui', 'sans-serif'],
      },
    },
  },
  plugins: [],
};
