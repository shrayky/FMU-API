import { defineConfig } from 'vite';
import { resolve } from 'path';

export default defineConfig({
  // Базовый путь для сборки
  base: './',
  
  // Директория с исходным кодом
  root: './',
  
  // Настройки сборки
  build: {
    // Директория для выходных файлов
    outDir: 'bundle',
    emptyOutDir: true,
    
    // Настройки для объединения всех модулей в один файл
    rollupOptions: {
      input: './js/views/index.js',
      output: {
        // Создаем один файл для всего кода
        format: 'iife',
        // Добавляем хеширование для лучшего кеширования
        entryFileNames: 'index.js',
        // Отключаем разделение на чанки
        manualChunks: undefined,
        // Объединяем все зависимости в один файл
        inlineDynamicImports: true
      }
    }
  }
});