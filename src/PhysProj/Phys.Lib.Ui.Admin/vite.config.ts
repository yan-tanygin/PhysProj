import { sveltekit } from '@sveltejs/kit/vite';
import { defineConfig } from 'vite';

export default defineConfig({
	plugins: [sveltekit()],
	server: {
		proxy: {
		  "/api": {
			target: "https://localhost:7188",
			changeOrigin: true,
			secure: false,
		  },
		},
	  },
});