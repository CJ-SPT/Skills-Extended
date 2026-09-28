// npm install sharp@0.34.3, or point NODE_PATH at an existing sharp installation.
const fs = require('fs');
const path = require('path');
const sharp = require('sharp');
(async () => {
  const root = path.join(__dirname, 'Inputs');
  for (const name of fs.readdirSync(root).filter(x => x.endsWith('.svg'))) {
    const source = fs.readFileSync(path.join(root, name), 'utf8').replaceAll('currentColor', '#ffffff');
    await sharp(Buffer.from(source), { density: 384 }).resize(128, 128).png().toFile(path.join(root, name.replace('.svg', '.png')));
  }
})().catch(e => { console.error(e); process.exit(1); });
