/** @type {import('tailwindcss').Config} */
module.exports = {
  content: ["index.html", "changelog.html", "./node_modules/flowbite/**/*.js"],
  theme: {
    extend: {
      colors: {
        primary: { "50": "#f3f0ff", "100": "#e9e2ff", "200": "#d3c9ff", "300": "#b49bff", "400": "#9068ff", "500": "#7e3af2", "600": "#6c2bd9", "700": "#5521b5", "800": "#441b93", "900": "#361778" }
      }
    },
  },
  plugins: [
    require('flowbite/plugin')
  ],
}
