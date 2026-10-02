// Generates the PWA icon set from public/logo.png.
//
// WHY THIS EXISTS
// The manifest previously declared /logo.png as both a 192x192 and a 512x512 icon.
// The file is actually 595x420, so neither declaration was true. Chrome's
// installability check validates the declared size against the real bitmap and
// refuses to offer installation, which is why the app was never installable.
//
// The previous manifest also used a single non-square image with
// purpose "any maskable". A maskable icon is cropped by the launcher (Android
// applies a circle/squircle that can remove up to 20% of each edge), so the logo
// has to be inset into a padded square to survive the crop. Two separate files
// are produced for that reason.
//
// NO EXTERNAL DEPENDENCIES. This deployment is air-gapped and the rule for this
// repo is "do not introduce unnecessary dependencies", so this deliberately does
// not use `canvas`/`sharp`. Node's built-in `zlib` is enough to emit a valid PNG:
// a signature, an IHDR chunk, a zlib-compressed IDAT of filtered scanlines, and
// an IEND chunk.
//
//   node scripts/generate-pwa-icons.mjs

import { deflateSync, inflateSync } from 'node:zlib';
import { mkdir, readFile, writeFile } from 'node:fs/promises';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

const here = dirname(fileURLToPath(import.meta.url));
const publicDir = join(here, '..', 'public');
const outDir = join(publicDir, 'icons');

// Brand colour, must match theme.ts primary.main and the manifest theme_color.
const BRAND = [0x57, 0x64, 0x26];

// ── Minimal PNG decoder (8-bit, non-interlaced) ─────────────────────────────
// Supports exactly what logo.png is. Anything else throws loudly rather than
// silently producing a blank icon.
function decodePng(buffer) {
  const SIG = Buffer.from([0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a]);
  if (!buffer.subarray(0, 8).equals(SIG)) throw new Error('not a PNG');

  let offset = 8;
  let width = 0;
  let height = 0;
  let bitDepth = 0;
  let colorType = 0;
  const idat = [];

  while (offset < buffer.length) {
    const length = buffer.readUInt32BE(offset);
    const type = buffer.toString('ascii', offset + 4, offset + 8);
    const data = buffer.subarray(offset + 8, offset + 8 + length);
    offset += 12 + length; // length + type + data + crc

    if (type === 'IHDR') {
      width = data.readUInt32BE(0);
      height = data.readUInt32BE(4);
      bitDepth = data[8];
      colorType = data[9];
      if (data[12] !== 0) throw new Error('interlaced PNG is not supported');
    } else if (type === 'IDAT') {
      idat.push(data);
    } else if (type === 'IEND') {
      break;
    }
  }

  if (bitDepth !== 8) throw new Error(`unsupported bit depth ${bitDepth}`);
  // 6 = RGBA, 2 = RGB, 0 = grey, 4 = grey+alpha.
  const channels = { 0: 1, 2: 3, 4: 2, 6: 4 }[colorType];
  if (!channels) throw new Error(`unsupported colour type ${colorType}`);

  const raw = inflateSync(Buffer.concat(idat));
  const stride = width * channels;
  const pixels = Buffer.alloc(height * stride);

  let pos = 0;
  for (let y = 0; y < height; y++) {
    const filter = raw[pos++];
    const line = raw.subarray(pos, pos + stride);
    pos += stride;

    const out = pixels.subarray(y * stride, (y + 1) * stride);
    const prior = y > 0 ? pixels.subarray((y - 1) * stride, y * stride) : null;

    for (let x = 0; x < stride; x++) {
      const a = x >= channels ? out[x - channels] : 0;
      const b = prior ? prior[x] : 0;
      const c = prior && x >= channels ? prior[x - channels] : 0;
      let value = line[x];

      switch (filter) {
        case 0: break;
        case 1: value += a; break;
        case 2: value += b; break;
        case 3: value += (a + b) >> 1; break;
        case 4: {
          const p = a + b - c;
          const pa = Math.abs(p - a);
          const pb = Math.abs(p - b);
          const pc = Math.abs(p - c);
          value += pa <= pb && pa <= pc ? a : pb <= pc ? b : c;
          break;
        }
        default: throw new Error(`unknown PNG filter ${filter}`);
      }
      out[x] = value & 0xff;
    }
  }

  return { width, height, channels, pixels };
}

// ── Minimal PNG encoder (8-bit RGBA, filter 0) ──────────────────────────────
function crc32(buf) {
  let c = ~0;
  for (let i = 0; i < buf.length; i++) {
    c ^= buf[i];
    for (let k = 0; k < 8; k++) c = (c >>> 1) ^ (0xedb88320 & -(c & 1));
  }
  return ~c >>> 0;
}

function chunk(type, data) {
  const length = Buffer.alloc(4);
  length.writeUInt32BE(data.length, 0);
  const typeAndData = Buffer.concat([Buffer.from(type, 'ascii'), data]);
  const crc = Buffer.alloc(4);
  crc.writeUInt32BE(crc32(typeAndData), 0);
  return Buffer.concat([length, typeAndData, crc]);
}

function encodePng(width, height, rgba) {
  const stride = width * 4;
  // Each scanline is prefixed with filter type 0 (None).
  const raw = Buffer.alloc(height * (stride + 1));
  for (let y = 0; y < height; y++) {
    raw[y * (stride + 1)] = 0;
    rgba.copy(raw, y * (stride + 1) + 1, y * stride, (y + 1) * stride);
  }

  const ihdr = Buffer.alloc(13);
  ihdr.writeUInt32BE(width, 0);
  ihdr.writeUInt32BE(height, 4);
  ihdr[8] = 8;  // bit depth
  ihdr[9] = 6;  // colour type RGBA
  ihdr[10] = 0; // compression
  ihdr[11] = 0; // filter
  ihdr[12] = 0; // interlace

  return Buffer.concat([
    Buffer.from([0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a]),
    chunk('IHDR', ihdr),
    chunk('IDAT', deflateSync(raw, { level: 9 })),
    chunk('IEND', Buffer.alloc(0)),
  ]);
}

/** Nearest-neighbour RGBA read from the decoded source image. */
function sample(image, x, y) {
  const { width, height, channels, pixels } = image;
  const cx = Math.min(width - 1, Math.max(0, Math.round(x)));
  const cy = Math.min(height - 1, Math.max(0, Math.round(y)));
  const i = (cy * width + cx) * channels;

  if (channels === 4) return [pixels[i], pixels[i + 1], pixels[i + 2], pixels[i + 3]];
  if (channels === 3) return [pixels[i], pixels[i + 1], pixels[i + 2], 255];
  if (channels === 2) return [pixels[i], pixels[i], pixels[i], pixels[i + 1]];
  return [pixels[i], pixels[i], pixels[i], 255];
}

/**
 * Composites the logo onto a square canvas.
 * @param insetFraction fraction of the canvas the logo may occupy
 * @param background    RGB triple painted first, or null to keep transparency
 */
function render(image, size, { insetFraction = 1, background = null }) {
  const out = Buffer.alloc(size * size * 4);

  const box = size * insetFraction;
  const scale = Math.min(box / image.width, box / image.height);
  const w = image.width * scale;
  const h = image.height * scale;
  const offsetX = (size - w) / 2;
  const offsetY = (size - h) / 2;

  for (let y = 0; y < size; y++) {
    for (let x = 0; x < size; x++) {
      const i = (y * size + x) * 4;

      // Background layer, if any. Maskable and iOS icons paint the brand colour so
      // the launcher has an opaque square to crop; the "any" icons leave it out and
      // keep the logo's own transparency.
      let r = background ? background[0] : 0;
      let g = background ? background[1] : 0;
      let b = background ? background[2] : 0;
      let a = background ? 255 : 0;

      const sx = (x - offsetX) / scale;
      const sy = (y - offsetY) / scale;
      if (sx >= 0 && sy >= 0 && sx < image.width && sy < image.height) {
        const [sr, sg, sb, sa] = sample(image, sx, sy);

        // Standard source-over: result alpha is the union of the two layers.
        const srcA = sa / 255;
        const dstA = a / 255;
        const outA = srcA + dstA * (1 - srcA);

        if (outA > 0) {
          r = Math.round((sr * srcA + r * dstA * (1 - srcA)) / outA);
          g = Math.round((sg * srcA + g * dstA * (1 - srcA)) / outA);
          b = Math.round((sb * srcA + b * dstA * (1 - srcA)) / outA);
        }
        a = Math.round(outA * 255);
      }

      out[i] = r;
      out[i + 1] = g;
      out[i + 2] = b;
      out[i + 3] = a;
    }
  }

  return encodePng(size, size, out);
}

const image = decodePng(await readFile(join(publicDir, 'logo.png')));
console.log(`source logo.png: ${image.width}x${image.height}, ${image.channels} channel(s)`);

await mkdir(outDir, { recursive: true });

const outputs = [
  // "any": launcher may letterbox these, so the logo fills most of the canvas.
  { file: 'icon-192.png', size: 192, opts: { insetFraction: 0.9 } },
  { file: 'icon-512.png', size: 512, opts: { insetFraction: 0.9 } },

  // "maskable": content stays inside the middle ~72% so a circular/squircle crop
  // cannot clip it, with an opaque brand background behind it.
  { file: 'icon-maskable-192.png', size: 192, opts: { insetFraction: 0.72, background: BRAND } },
  { file: 'icon-maskable-512.png', size: 512, opts: { insetFraction: 0.72, background: BRAND } },

  // iOS home-screen icon: must be opaque; iOS does not composite transparency.
  { file: 'apple-touch-icon-180.png', size: 180, opts: { insetFraction: 0.85, background: BRAND } },
];

for (const { file, size, opts } of outputs) {
  const png = render(image, size, opts);
  await writeFile(join(outDir, file), png);
  console.log(`wrote icons/${file} (${size}x${size}, ${png.length} bytes)`);
}