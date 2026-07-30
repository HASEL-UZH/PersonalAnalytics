import js from '@eslint/js';
import prettierConfig from '@vue/eslint-config-prettier';
import { withVueTs, vueTsConfigs } from '@vue/eslint-config-typescript';
import pluginVue from 'eslint-plugin-vue';
import globals from 'globals';

export default withVueTs(
  {
    ignores: ['node_modules/**', '**/dist/**', 'dist-electron/**', 'release/**', '.gitignore'],
  },
  {
    languageOptions: {
      globals: globals.node,
    },
  },
  js.configs.recommended,
  pluginVue.configs['flat/recommended'],
  vueTsConfigs.recommended,
  {
    files: ['**/*.cjs'],
    rules: {
      '@typescript-eslint/no-require-imports': 'off',
    },
  },
  prettierConfig,
);
