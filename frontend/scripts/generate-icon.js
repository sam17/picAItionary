const { createCanvas } = require('canvas');
const fs = require('fs');
const path = require('path');

// Create a 32x32 canvas for the favicon
const canvas = createCanvas(32, 32);
const ctx = canvas.getContext('2d');

// Set background
ctx.fillStyle = '#4F46E5'; // Indigo color
ctx.fillRect(0, 0, 32, 32);

// Draw bot face
ctx.fillStyle = '#FFFFFF';
// Eyes
ctx.fillRect(8, 8, 4, 4);
ctx.fillRect(20, 8, 4, 4);
// Mouth
ctx.fillRect(8, 20, 16, 2);

// Save the image
const buffer = canvas.toBuffer('image/png');
fs.writeFileSync(path.join(__dirname, '../public/bot-icon.png'), buffer); 