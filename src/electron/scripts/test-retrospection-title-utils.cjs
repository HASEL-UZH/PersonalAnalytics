const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const ts = require('typescript');
const vm = require('node:vm');

const sourcePath = path.join(
  __dirname,
  '..',
  'electron',
  'main',
  'services',
  'RetrospectionService.ts'
);
const source = `${fs.readFileSync(sourcePath, 'utf8')}
export {
  cleanWindowTitle,
  getReadableUrlTitle,
  isBrowserProcessName,
  removeGenericBrowserTabCountFragments,
  stripPathFragment
};
`;
const { outputText } = ts.transpileModule(source, {
  compilerOptions: {
    module: ts.ModuleKind.CommonJS,
    target: ts.ScriptTarget.ES2022
  }
});
const moduleContext = { exports: {} };

vm.runInNewContext(outputText, {
  module: moduleContext,
  exports: moduleContext.exports,
  require: (request) => {
    if (request.includes('UserInputEntity')) {
      return { UserInputEntity: {} };
    }
    if (request.includes('WindowActivityEntity')) {
      return { WindowActivityEntity: {} };
    }
    if (request.includes('Logger')) {
      return {
        getMainLogger: () => ({
          warn: () => undefined,
          error: () => undefined
        })
      };
    }
    return require(request);
  },
  URL
});

const {
  cleanWindowTitle,
  getReadableUrlTitle,
  isBrowserProcessName,
  removeGenericBrowserTabCountFragments,
  stripPathFragment
} = moduleContext.exports;

const cases = [
  {
    name: 'visible URL titles keep the old compact format without ellipsis',
    actual: getReadableUrlTitle('github.com/HASEL-UZH/PersonalAnalytics/pull/123'),
    expected: 'github.com/pull/123'
  },
  {
    name: 'hover URL titles include ellipsis when shortened',
    actual: getReadableUrlTitle('github.com/HASEL-UZH/PersonalAnalytics/pull/123', true),
    expected: 'github.com/.../pull/123'
  },
  {
    name: 'unshortened URL paths do not include ellipsis',
    actual: getReadableUrlTitle('github.com/pull/123'),
    expected: 'github.com/pull/123'
  },
  {
    name: 'visible URL-only window titles keep the old compact format without ellipsis',
    actual: cleanWindowTitle(
      'github.com/HASEL-UZH/PersonalAnalytics/pull/123',
      'Microsoft Edge',
      'https://github.com/HASEL-UZH/PersonalAnalytics/pull/123'
    ),
    expected: 'github.com/pull/123'
  },
  {
    name: 'hover URL-only window titles include ellipsis when shortened',
    actual: cleanWindowTitle(
      'github.com/HASEL-UZH/PersonalAnalytics/pull/123',
      'Microsoft Edge',
      'https://github.com/HASEL-UZH/PersonalAnalytics/pull/123',
      true
    ),
    expected: 'github.com/.../pull/123'
  },
  {
    name: 'generic browser prefix is removed from hover label',
    actual: cleanWindowTitle(
      '4 or more pages - Overleaf - Microsoft Edge',
      'Microsoft Edge',
      'https://overleaf.com/project/mock'
    ),
    expected: 'Overleaf'
  },
  {
    name: 'generic browser middle fragment is removed from hover label',
    actual: cleanWindowTitle(
      'Overleaf - 4 or more pages - Microsoft Edge',
      'Microsoft Edge',
      'https://overleaf.com/project/mock'
    ),
    expected: 'Overleaf'
  },
  {
    name: 'embedded browser tab-count fragment is removed from hover label',
    actual: cleanWindowTitle(
      'Overleaf 4 or more pages - Microsoft Edge',
      'Microsoft Edge',
      'https://overleaf.com/project/mock'
    ),
    expected: 'Overleaf'
  },
  {
    name: 'generic-only browser title falls back to captured domain',
    actual: cleanWindowTitle(
      '4 or more pages - Microsoft Edge',
      'Microsoft Edge',
      'https://overleaf.com/project/mock'
    ),
    expected: 'overleaf.com'
  },
  {
    name: 'visible file paths keep the old final-segment format without ellipsis',
    actual: stripPathFragment('vim ~/code/activitywatch/aw-server/file.py'),
    expected: 'vim file.py'
  },
  {
    name: 'hover file paths include ellipsis when shortened',
    actual: stripPathFragment('vim ~/code/activitywatch/aw-server/file.py', true),
    expected: 'vim ~/.../file.py'
  },
  {
    name: 'visible windows file paths keep the old final-segment format without ellipsis',
    actual: stripPathFragment('C:\\Users\\username\\DevEx'),
    expected: 'DevEx'
  },
  {
    name: 'hover windows file paths include ellipsis when shortened',
    actual: stripPathFragment('C:\\Users\\username\\DevEx', true),
    expected: 'C:/.../DevEx'
  },
  {
    name: 'visible windows file paths with spaces keep the old final-segment format',
    actual: stripPathFragment('C:\\DevEx in Practice2026-DXIP-MAXXQDA.mxqda'),
    expected: 'DevEx in Practice2026-DXIP-MAXXQDA.mxqda'
  },
  {
    name: 'hover windows file paths with spaces include ellipsis when shortened',
    actual: stripPathFragment('C:\\DevEx in Practice2026-DXIP-MAXXQDA.mxqda', true),
    expected: 'C:/.../DevEx in Practice2026-DXIP-MAXXQDA.mxqda'
  },
  {
    name: 'MAXQDA-style path title keeps visible label without explicit ellipsis',
    actual: cleanWindowTitle(
      'C:\\DevEx in Practice2026-DXIP-MAXXQDA.mxqda - MAXQDA Analytics Pro (26.2.1) - Read/Write Document',
      'MAXQDA Analytics Pro'
    ),
    expected: 'DevEx in Practice2026-DXIP-MAXXQDA.mxqda'
  },
  {
    name: 'MAXQDA-style path title is shortened for hover labels',
    actual: cleanWindowTitle(
      'C:\\DevEx in Practice2026-DXIP-MAXXQDA.mxqda - MAXQDA Analytics Pro (26.2.1) - Read/Write Document',
      'MAXQDA Analytics Pro',
      null,
      true
    ),
    expected: 'C:/.../DevEx in Practice2026-DXIP-MAXXQDA.mxqda'
  },
  {
    name: 'tab-count removal handles alternative browser wording',
    actual: removeGenericBrowserTabCountFragments('Notion - 4 other pages'),
    expected: 'Notion'
  },
  {
    name: 'Obsidian is not treated as Dia browser',
    actual: isBrowserProcessName('Obsidian'),
    expected: false
  },
  {
    name: 'Dia remains a recognized browser',
    actual: isBrowserProcessName('Dia'),
    expected: true
  },
  {
    name: 'Microsoft Edge remains a recognized browser',
    actual: isBrowserProcessName('Microsoft Edge'),
    expected: true
  },
  {
    name: 'MAXQDA forward-slash path with version suffix is shortened to filename',
    actual: cleanWindowTitle(
      'C:/Users/<username>/OneDrive//Research/Project/2026-Project.mqda - MAXQDA Analytics Pro (26.2.1)',
      'MAXQDA Analytics Pro'
    ),
    expected: '2026-Project.mqda'
  },
  {
    name: 'Edge grouped-tabs title strips tab-count fragment',
    actual: cleanWindowTitle(
      'Structure - Chapters & Plays - Google Docs and 2 more pages - School - Microsoft​ Edge',
      'Microsoft Edge',
      null
    ),
    expected: 'Structure - Chapters & Plays - Google Docs - School - Microsoft​ Edge'
  },
  {
    name: 'Edge grouped-tabs with GitHub PR strips tab-count fragment',
    actual: cleanWindowTitle(
      '[#515] Add top websites and window titles by grigor-dochev · Pull Request #522 · HASEL-UZH/PersonalAnalytics and 1 more page - School - Microsoft​ Edge',
      'Microsoft Edge',
      null
    ),
    expected: '[#515] Add top websites and window titles by grigor-dochev · Pull Request #522 · HASEL-UZH/PersonalAnalytics - School - Microsoft​ Edge'
  },
  {
    name: 'Edge grouped-tabs with GitHub PR strips tab-count fragment',
    actual: cleanWindowTitle(
      'Untitled and 5 more pages - Personal - Microsoft​ Edge',
      'Microsoft Edge',
      null
    ),
    expected: 'Personal - Microsoft​ Edge' // i am not sure about what should be returned here, as it's basically not useful either way...
  }
];

for (const testCase of cases) {
  assert.equal(testCase.actual, testCase.expected, testCase.name);
}

console.log(`${cases.length} retrospection title utility tests passed`);
