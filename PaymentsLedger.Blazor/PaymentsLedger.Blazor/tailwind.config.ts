import type { Config } from "tailwindcss";

const config: Config = {
  content: [
    "./**/*.{razor,cshtml,html}",
    "./**/*.{js,ts}",
    "../Shared/**/*.{razor,cshtml,html}",
  ],
  theme: {
    extend: {},
  },
  plugins: [],
};

export default config;
