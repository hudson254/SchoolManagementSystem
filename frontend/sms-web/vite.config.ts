import { defineConfig } from 'vitest/config';
import react from '@vitejs/plugin-react';
import { fileURLToPath, URL } from 'node:url';
import { existsSync, readFileSync, readdirSync, writeFileSync } from 'node:fs';
import { join } from 'node:path';

/**
 * Stamps a per-build cache version into the emitted service worker.
 *
 * WHY THIS EXISTS
 * The service worker used a hard-coded `CACHE_NAME = 'sms-cache-v1'`. That name was
 * identical for every deployment, so a new release installed a worker that wrote
 * into the same cache the previous one had populated, and the activate step
 * (`keys.filter(k => k !== CACHE_NAME)`) had nothing to delete. Combined with nginx
 * serving sw.js as `expires 1y; immutable`, users could be pinned to one frontend
 * version indefinitely.
 *
 * HOW IT WORKS
 * The rewrite runs in `closeBundle`, against the files that were actually written to
 * the output directory. An earlier attempt used `generateBundle` and inspected the
 * in-memory bundle, but assets copied from `public/` are attached late in the
 * pipeline and the placeholder was never replaced - which shipped a worker
 * referencing an undefined CACHE_NAME and threw on first use. Reading the real
 * output files is independent of when the bundler attaches them.
 *
 * The id is an FNV-1a hash of the built index.html, so it is derived from CONTENT:
 * two builds of identical source produce the same version, while any change to the
 * emitted HTML produces a new cache and therefore invalidates the previous one.
 */
function serviceWorkerVersionPlugin() {
  return {
    name: 'sms-sw-version',
    apply: 'build' as const,
    closeBundle() {
      const outDir = resolveOutDir();
      const swPath = join(outDir, 'sw.js');
      const indexPath = join(outDir, 'index.html');

      if (!existsSync(swPath)) {
        this.warn('sw.js not found in the output directory; skipping cache-version stamp.');
        return;
      }

      const sw = readFileSync(swPath, 'utf8');
      if (!sw.includes('__SW_VERSION__')) {
        // Either already stamped (incremental rebuild) or the placeholder was
        // removed. Either way there is nothing to do.
        return;
      }

      // The basis for the version is the built index.html PLUS the sorted list of
      // emitted asset filenames. Using index.html alone is not enough: a change
      // confined to a lazily-loaded chunk can leave index.html byte-identical, and
      // the cache name would then not change. Asset filenames are content-hashed by
      // Vite, so including them makes any real code change produce a new version.
      const assetNames = existsSync(join(outDir, 'assets'))
        ? readdirSync(join(outDir, 'assets')).sort()
        : [];

      const basis = [
        existsSync(indexPath) ? readFileSync(indexPath, 'utf8') : '',
        assetNames.join('\n'),
      ].join('\n');

      const cacheName = `sms-shell-${simpleHash(basis)}`;

      writeFileSync(
        swPath,
        sw.replace('__SW_VERSION__', `const CACHE_NAME = '${cacheName}';`),
        'utf8'
      );
    },
  };
}

/** Resolves the absolute output directory, honouring a non-default `outDir`. */
function resolveOutDir(): string {
  const configured = process.env.SMS_BUILD_OUT_DIR;
  if (configured) return configured;

  // vite.config.ts declares outDir: 'dist' relative to the project root.
  return fileURLToPath(new URL('./dist', import.meta.url));
}

/** Small, stable, non-cryptographic hash (FNV-1a). Used only for cache busting. */
function simpleHash(input: string): string {
  let h = 0x811c9dc5;
  for (let i = 0; i < input.length; i++) {
    h ^= input.charCodeAt(i);
    h = Math.imul(h, 0x01000193);
  }
  return (h >>> 0).toString(36);
}

export default defineConfig({
  plugins: [react(), serviceWorkerVersionPlugin()],
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url)),
      '@components': fileURLToPath(new URL('./src/components', import.meta.url)),
      '@pages': fileURLToPath(new URL('./src/pages', import.meta.url)),
      '@hooks': fileURLToPath(new URL('./src/hooks', import.meta.url)),
      '@services': fileURLToPath(new URL('./src/services', import.meta.url)),
      '@utils': fileURLToPath(new URL('./src/utils', import.meta.url)),
      '@contexts': fileURLToPath(new URL('./src/contexts', import.meta.url)),
      '@types': fileURLToPath(new URL('./src/types', import.meta.url)),
    },
  },
  server: {
    port: 3000,
    proxy: {
      '/api': {
        target: 'https://localhost:5001',
        changeOrigin: true,
        secure: false,
      },
    },
  },
  build: {
    outDir: 'dist',
    // RISK-17: source maps expose the original TypeScript source in the
    // production bundle. They are only useful for local debugging, so they
    // are disabled for production builds. Dev/preview sourcemaps remain
    // available through the Vite dev server.
    sourcemap: false,
    rollupOptions: {
      output: {
        manualChunks(id: string) {
          if (id.includes('node_modules')) {
            if (id.includes('@mui') || id.includes('@emotion')) {
              return 'mui';
            }
            if (id.includes('@tanstack') || id.includes('react-query')) {
              return 'query';
            }
            return 'vendor';
          }
        },
      },
    },
  },
  test: {
    globals: true,
    environment: 'jsdom',
    setupFiles: './src/test/setup.ts',
    coverage: {
      provider: 'v8',
      reporter: ['text', 'json', 'html'],
    },
  },
});

