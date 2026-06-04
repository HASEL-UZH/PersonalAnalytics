import { expect, jest, test } from '@jest/globals';

jest.unstable_mockModule('electron', () => ({
  default: {
    app: {
      isPackaged: false
    }
  }
}));

const {
  cleanWindowTitle,
  getReadableUrlTitle,
  isBrowserProcessName,
  removeGenericBrowserTabCountFragments,
  stripPathFragment
} = await import('../services/RetrospectionService');

/**
 * The structure of this test array is as follows:
 * Index 0: test name
 * Index 1: actual cleaned title
 * Index 2: expected cleaned title
 */
const cases: [string, string | boolean | null, string | boolean | null][] = [
  [
    'visible URL titles keep the old compact format without ellipsis',
    getReadableUrlTitle('github.com/HASEL-UZH/PersonalAnalytics/pull/123'),
    'github.com/pull/123'
  ],
  [
    'hover URL titles include ellipsis when shortened',
    getReadableUrlTitle('github.com/HASEL-UZH/PersonalAnalytics/pull/123', true),
    'github.com/.../pull/123'
  ],
  [
    'unshortened URL paths do not include ellipsis',
    getReadableUrlTitle('github.com/pull/123'),
    'github.com/pull/123'
  ],
  [
    'visible URL-only window titles keep the old compact format without ellipsis',
    cleanWindowTitle(
      'github.com/HASEL-UZH/PersonalAnalytics/pull/123',
      'Microsoft Edge',
      'https://github.com/HASEL-UZH/PersonalAnalytics/pull/123'
    ),
    'github.com/pull/123'
  ],
  [
    'hover URL-only window titles include ellipsis when shortened',
    cleanWindowTitle(
      'github.com/HASEL-UZH/PersonalAnalytics/pull/123',
      'Microsoft Edge',
      'https://github.com/HASEL-UZH/PersonalAnalytics/pull/123',
      true
    ),
    'github.com/.../pull/123'
  ],
  [
    'generic browser prefix is removed from hover label',
    cleanWindowTitle(
      '4 or more pages - Overleaf - Microsoft Edge',
      'Microsoft Edge',
      'https://overleaf.com/project/mock'
    ),
    'Overleaf'
  ],
  [
    'generic browser middle fragment is removed from hover label',
    cleanWindowTitle(
      'Overleaf - 4 or more pages - Microsoft Edge',
      'Microsoft Edge',
      'https://overleaf.com/project/mock'
    ),
    'Overleaf'
  ],
  [
    'embedded browser tab-count fragment is removed from hover label',
    cleanWindowTitle(
      'Overleaf 4 or more pages - Microsoft Edge',
      'Microsoft Edge',
      'https://overleaf.com/project/mock'
    ),
    'Overleaf'
  ],
  [
    'generic-only browser title falls back to captured domain',
    cleanWindowTitle(
      '4 or more pages - Microsoft Edge',
      'Microsoft Edge',
      'https://overleaf.com/project/mock'
    ),
    'overleaf.com'
  ],
  [
    'visible file paths keep the old final-segment format without ellipsis',
    stripPathFragment('vim ~/code/activitywatch/aw-server/file.py'),
    'vim file.py'
  ],
  [
    'hover file paths include ellipsis when shortened',
    stripPathFragment('vim ~/code/activitywatch/aw-server/file.py', true),
    'vim ~/.../file.py'
  ],
  [
    'visible windows file paths keep the old final-segment format without ellipsis',
    stripPathFragment('C:\\Users\\username\\DevEx'),
    'DevEx'
  ],
  [
    'hover windows file paths include ellipsis when shortened',
    stripPathFragment('C:\\Users\\username\\DevEx', true),
    'C:/.../DevEx'
  ],
  [
    'visible windows file paths with spaces keep the old final-segment format',
    stripPathFragment('C:\\DevEx in Practice2026-DXIP-MAXXQDA.mxqda'),
    'DevEx in Practice2026-DXIP-MAXXQDA.mxqda'
  ],
  [
    'hover windows file paths with spaces include ellipsis when shortened',
    stripPathFragment('C:\\DevEx in Practice2026-DXIP-MAXXQDA.mxqda', true),
    'C:/.../DevEx in Practice2026-DXIP-MAXXQDA.mxqda'
  ],
  [
    'MAXQDA-style path title keeps visible label without explicit ellipsis',
    cleanWindowTitle(
      'C:\\DevEx in Practice2026-DXIP-MAXXQDA.mxqda - MAXQDA Analytics Pro (26.2.1) - Read/Write Document',
      'MAXQDA Analytics Pro',
      null,
      false,
      'ReadWriteDocument'
    ),
    'DevEx in Practice2026-DXIP-MAXXQDA.mxqda'
  ],
  [
    'MAXQDA-style path title is shortened for hover labels',
    cleanWindowTitle(
      'C:\\DevEx in Practice2026-DXIP-MAXXQDA.mxqda - MAXQDA Analytics Pro (26.2.1) - Read/Write Document',
      'MAXQDA Analytics Pro',
      null,
      true,
      'ReadWriteDocument'
    ),
    'C:/.../DevEx in Practice2026-DXIP-MAXXQDA.mxqda'
  ],
  [
    'tab-count removal handles alternative browser wording',
    removeGenericBrowserTabCountFragments('Notion - 4 other pages'),
    'Notion'
  ],
  [
    'tab-count removal handles and-more-pages without a number',
    removeGenericBrowserTabCountFragments('Google Docs and more pages'),
    'Google Docs'
  ],
  [
    'tab-count removal removes dangling and before separators',
    removeGenericBrowserTabCountFragments('Google Docs and - School'),
    'Google Docs - School'
  ],
  ['Obsidian is not treated as Dia browser', isBrowserProcessName('Obsidian'), false],
  ['Dia remains a recognized browser', isBrowserProcessName('Dia'), true],
  ['Microsoft Edge remains a recognized browser', isBrowserProcessName('Microsoft Edge'), true],
  [
    'MAXQDA forward-slash path with version suffix is shortened to filename',
    cleanWindowTitle(
      'C:/Users/<username>/OneDrive//Research/Project/2026-Project.mqda - MAXQDA Analytics Pro (26.2.1)',
      'MAXQDA Analytics Pro'
    ),
    '2026-Project.mqda'
  ],
  [
    'Edge grouped-tabs title strips and-more-pages fragment',
    cleanWindowTitle(
      'Structure - Chapters & Plays - Google Docs and 2 more pages - School - Microsoft\u200b Edge',
      'Microsoft Edge'
    ),
    'Structure - Chapters & Plays - Google Docs'
  ],
  [
    'Edge grouped-tabs title strips and-more-pages fragment without a number',
    cleanWindowTitle(
      'Structure - Chapters & Plays - Google Docs and more pages - School - Microsoft\u200b Edge',
      'Microsoft Edge'
    ),
    'Structure - Chapters & Plays - Google Docs'
  ],
  [
    'Edge grouped-tabs with GitHub PR strips and-more-page fragment',
    cleanWindowTitle(
      '[#515] Add top websites and window titles by grigor-dochev · Pull Request #522 · HASEL-UZH/PersonalAnalytics and 1 more page - School - Microsoft\u200b Edge',
      'Microsoft Edge'
    ),
    '[#515] Add top websites and window titles by grigor-dochev · Pull Request #522 · HASEL-UZH/PersonalAnalytics'
  ],
  [
    'Edge grouped-tabs title drops generic profile-only result',
    cleanWindowTitle(
      'Untitled and 5 more pages - Personal - Microsoft\u200b Edge',
      'Microsoft Edge'
    ),
    null
  ],
  [
    'Edge grouped-tabs title drops generic profile-only result without a number',
    cleanWindowTitle('Untitled and more pages - Personal - Microsoft\u200b Edge', 'Microsoft Edge'),
    null
  ],
  [
    'Chrome new-tab title falls back to captured domain',
    cleanWindowTitle('New Tab - Google Chrome', 'Google Chrome', 'https://calendar.google.com'),
    'calendar.google.com'
  ],
  [
    'Chrome grouped-tabs title strips and-more-pages fragment',
    cleanWindowTitle(
      'Inbox and 3 more pages - Gmail - Google Chrome',
      'Google Chrome',
      'https://mail.google.com'
    ),
    'Inbox - Gmail'
  ],
  [
    'Chrome grouped-tabs title strips and-more-pages fragment without a number',
    cleanWindowTitle(
      'Inbox and more pages - Gmail - Google Chrome',
      'Google Chrome',
      'https://mail.google.com'
    ),
    'Inbox - Gmail'
  ],
  [
    'Safari untitled title falls back to captured domain',
    cleanWindowTitle('Untitled - Safari', 'Safari', 'https://www.apple.com'),
    'apple.com'
  ],
  [
    'Safari grouped-tabs title strips and-more-pages fragment',
    cleanWindowTitle('Project plan and 4 more pages - Notion - Safari', 'Safari'),
    'Project plan - Notion'
  ],
  [
    'Safari grouped-tabs title strips and-more-pages fragment without a number',
    cleanWindowTitle('Project plan and more pages - Notion - Safari', 'Safari'),
    'Project plan - Notion'
  ]
];

test.each(cases)('.cleanWindowTitle helpers: %s', (_name, actual, expected) => {
  expect(actual).toBe(expected);
});
