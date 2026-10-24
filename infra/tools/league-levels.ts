// Fuente de puntuación: solo tutorial y mundos publicados, nunca reserva ni seasons.
import { readFileSync, writeFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { createHash } from 'node:crypto';
const dir = new URL('../../Assets/_Project/Data/Levels/', import.meta.url);
const files = ['tutorial_v0.json', ...Array.from({ length: 8 }, (_, i) => `mundo_${String(i + 2).padStart(2, '0')}_v1.json`)];
const levels: Record<string, { difficulty: string; points: number; skippable: boolean }> = {};
const sources: Record<string, string> = {};
for (const file of files) {
  const raw = readFileSync(new URL(file, dir));
  sources[file] = createHash('sha256').update(raw.toString().replace(/\r\n/g, '\n')).digest('hex');
  const pack = JSON.parse(raw.toString()) as { levels: Array<{ level: number; difficulty: { label: string }; skippable: boolean }> };
  for (const level of pack.levels) {
    // El tutorial no declara etiqueta; el cliente lo trata como Normal.
    const difficulty = level.difficulty.label ?? (file === 'tutorial_v0.json' ? 'normal' : '');
    const points = ({ normal: 10, hard: 20, very_hard: 30 } as Record<string, number>)[difficulty];
    if (!points || levels[level.level]) throw Error(`Nivel duplicado o dificultad desconocida: ${file}:${level.level}`);
    levels[level.level] = { difficulty, points, skippable: level.skippable ?? false };
  }
}
if (Object.keys(levels).length !== 700 || Array.from({ length: 700 }, (_, i) => i + 1).some(n => !levels[n])) throw Error('La campaña debe cubrir exactamente 1–700');
const output = new URL('../functions/league/levels.generated.json', import.meta.url);
const content = JSON.stringify({ sources, levels }, null, 2) + '\n';
if (process.argv.includes('--check')) {
  if (readFileSync(output, 'utf8').replace(/\r\n/g, '\n') !== content) throw Error(`Tabla desactualizada: ${fileURLToPath(output)}`);
  console.log('Liga: 700 niveles idénticos a los packs publicados.');
} else {
  writeFileSync(output, content);
  console.log('Liga: tabla de 700 niveles generada.');
}
