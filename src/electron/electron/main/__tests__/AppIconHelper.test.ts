import { beforeEach, expect, jest, test } from '@jest/globals';

const WORD_PROCESS_PATH = 'C:\\Program Files\\Microsoft Office\\root\\Office16\\WINWORD.EXE';
const WORD_MANIFEST_PATH =
  'C:\\Program Files\\Microsoft Office\\root\\Office16\\WINWORD.VisualElementsManifest.xml';
const WORD_ASSETS_DIRECTORY = 'C:\\Program Files\\Microsoft Office\\root\\Office16\\Assets';
const WORD_TARGET_ICON_PATH =
  'C:\\Program Files\\Microsoft Office\\root\\Office16\\Assets\\WordLogo.targetsize-32.png';
const SHELL_ICON_PROCESS_PATH = 'C:\\Tools\\ShellIconApp\\ShellIconApp.exe';
const SHELL_ICON_DATA_URL = 'data:image/png;base64,shell';
const OUTLOOK_PROCESS_PATH = 'C:\\Program Files\\Microsoft Office\\root\\Office16\\OUTLOOK.EXE';
const TEAMS_PROCESS_PATH = 'C:\\Users\\test\\AppData\\Local\\Microsoft\\Teams\\current\\Teams.exe';
const GITHUB_DESKTOP_PROCESS_PATH =
  'C:\\Users\\test\\AppData\\Local\\GitHubDesktop\\GitHubDesktop.exe';
const WINDOWS_APPS_FILE_ICON_PROCESS_PATH =
  'C:\\Program Files\\WindowsApps\\FallbackApp_1.0.0.0_x64__test\\FallbackApp.exe';
const WINDOWS_APPS_TEAMS_PROCESS_PATH =
  'C:\\Program Files\\WindowsApps\\MSTeams_26163.407.4851.7751_x64__8wekyb3d8bbwe\\ms-teams.exe';
const WINDOWS_APPS_WHATSAPP_PROCESS_PATH =
  'C:\\Program Files\\WindowsApps\\5319275A.WhatsAppDesktop_2.2625.101.0_x64__cv1g1gvanyjgm\\WhatsApp.Root.exe';
const WINDOWS_APPS_CLAUDE_PROCESS_PATH =
  'C:\\Program Files\\WindowsApps\\Claude_1.14271.0.0_x64__pzs8sxrjxfjjc\\app\\Claude.exe';
const WINDOWS_APPS_PACKAGE_ICON_CASES = [
  {
    name: 'Microsoft Teams',
    processPath: WINDOWS_APPS_TEAMS_PROCESS_PATH,
    processId: 11064
  },
  {
    name: 'WhatsApp',
    processPath: WINDOWS_APPS_WHATSAPP_PROCESS_PATH,
    processId: 21800
  },
  {
    name: 'Claude',
    processPath: WINDOWS_APPS_CLAUDE_PROCESS_PATH,
    processId: 35132
  }
];
const WINDOWS_NATIVE_ICON_PROCESS_PATHS = [
  OUTLOOK_PROCESS_PATH,
  TEAMS_PROCESS_PATH,
  GITHUB_DESKTOP_PROCESS_PATH,
  WINDOWS_APPS_FILE_ICON_PROCESS_PATH
];
const NATIVE_ICON_BUFFER = Buffer.from('native-file-icon');
const NATIVE_ICON_DATA_URL = 'data:image/png;base64,native-file-icon';
const WINDOWS_APPS_PACKAGE_ICON_BUFFER = Buffer.from('windows-app-package-icon');
const WINDOWS_APPS_PACKAGE_ICON_DATA_URL = 'data:image/png;base64,windows-app-package-icon';
const MAC_WORD_PROCESS_PATH = '/Applications/Microsoft Word.app/Contents/MacOS/Microsoft Word';
const MAC_WORD_INFO_PLIST_PATH = '/Applications/Microsoft Word.app/Contents/Info.plist';
const MAC_WORD_RESOURCES_PATH = '/Applications/Microsoft Word.app/Contents/Resources';
const MAC_WORD_ICON_PATH = '/Applications/Microsoft Word.app/Contents/Resources/Word_macOS.icns';
const MAC_TEAMS_PROCESS_PATH = '/Applications/Microsoft Teams.app/Contents/MacOS/Microsoft Teams';
const MAC_TEAMS_APP_BUNDLE_PATH = '/Applications/Microsoft Teams.app';
const SIPS_TEMP_DIRECTORY = '/tmp/personal-analytics-icon-test';
const SIPS_PNG_PATH = '/tmp/personal-analytics-icon-test/icon.png';

const getFileIconMock = jest.fn((filePath: string) =>
  Promise.resolve({
    isEmpty: () => filePath !== SHELL_ICON_PROCESS_PATH,
    resize: () => ({
      toDataURL: () => SHELL_ICON_DATA_URL
    })
  })
);

const resizeMock = jest.fn(() => ({
  toDataURL: () => 'data:image/png;base64,word'
}));

const createFromPathMock = jest.fn((iconPath: string) => ({
  isEmpty: () => iconPath !== WORD_TARGET_ICON_PATH,
  resize: resizeMock
}));

const createFromBufferMock = jest.fn((buffer: Buffer) => ({
  isEmpty: () => false,
  resize: () => ({
    toDataURL: () => {
      if (buffer.equals(NATIVE_ICON_BUFFER)) {
        return NATIVE_ICON_DATA_URL;
      }

      if (buffer.equals(WINDOWS_APPS_PACKAGE_ICON_BUFFER)) {
        return WINDOWS_APPS_PACKAGE_ICON_DATA_URL;
      }

      return 'data:image/png;base64,mac-word';
    }
  })
}));

const getPackageIconMock = jest.fn((_: number, processPath: string) => {
  return WINDOWS_APPS_PACKAGE_ICON_CASES.some((app) => app.processPath === processPath)
    ? WINDOWS_APPS_PACKAGE_ICON_BUFFER
    : undefined;
});

const extractFileIconMock = Object.assign(
  jest.fn((iconPath: string) => {
    if (
      WINDOWS_NATIVE_ICON_PROCESS_PATHS.includes(iconPath) ||
      iconPath === MAC_TEAMS_APP_BUNDLE_PATH
    ) {
      return NATIVE_ICON_BUFFER;
    }

    return undefined;
  }),
  { getPackageIcon: getPackageIconMock }
);

const existsSyncMock = jest.fn((filePath: string) => {
  return [
    WORD_MANIFEST_PATH,
    WORD_ASSETS_DIRECTORY,
    WORD_TARGET_ICON_PATH,
    MAC_WORD_INFO_PLIST_PATH,
    MAC_WORD_RESOURCES_PATH,
    MAC_WORD_ICON_PATH
  ].includes(filePath.toString());
});

const readFileSyncMock = jest.fn((filePath: string) => {
  if (filePath.toString() === MAC_WORD_INFO_PLIST_PATH) {
    return `
      <plist>
        <dict>
          <key>CFBundleIconFile</key>
          <string>Word_macOS</string>
        </dict>
      </plist>
    `;
  }

  if (filePath.toString() === SIPS_PNG_PATH) {
    return Buffer.from('converted-png');
  }

  if (filePath.toString() === WORD_MANIFEST_PATH) {
    return `
      <Application>
        <VisualElements
          Square44x44Logo="Assets\\WordLogo.png"
          Square150x150Logo="Assets\\WordTile.png"
          ShowNameOnSquare150x150Logo="on" />
      </Application>
    `;
  }

  throw new Error(`Unexpected file read: ${filePath.toString()}`);
});

const readdirSyncMock = jest.fn((directoryPath: string) => {
  if (directoryPath.toString() === MAC_WORD_RESOURCES_PATH) {
    return ['Word_macOS.icns', 'WXBN.icns'];
  }

  if (directoryPath.toString() !== WORD_ASSETS_DIRECTORY) {
    throw new Error(`Unexpected directory read: ${directoryPath.toString()}`);
  }

  return ['WordLogo.scale-200.png', 'WordLogo.targetsize-32.png', 'WordLogo.targetsize-16.png'];
});

const mkdtempSyncMock = jest.fn(() => SIPS_TEMP_DIRECTORY);
const rmSyncMock = jest.fn();
const execFileSyncMock = jest.fn();

jest.unstable_mockModule('electron', () => ({
  app: {
    getFileIcon: getFileIconMock
  },
  nativeImage: {
    createFromPath: createFromPathMock,
    createFromBuffer: createFromBufferMock
  },
  default: {
    app: {
      isPackaged: false
    }
  }
}));

jest.unstable_mockModule('node:fs', () => ({
  existsSync: existsSyncMock,
  mkdtempSync: mkdtempSyncMock,
  readFileSync: readFileSyncMock,
  readdirSync: readdirSyncMock,
  rmSync: rmSyncMock
}));

jest.unstable_mockModule('node:child_process', () => ({
  execFileSync: execFileSyncMock
}));

jest.unstable_mockModule('extract-file-icon', () => ({
  default: extractFileIconMock
}));

const { getProcessIconDataUrl } = await import('../services/utils/AppIconHelper');

// AppIconHelper gates its macOS code paths on process.platform, so those tests have to fake it.
async function withPlatform(platform: NodeJS.Platform, run: () => Promise<void>): Promise<void> {
  const originalPlatform = process.platform;
  Object.defineProperty(process, 'platform', { value: platform, configurable: true });

  try {
    await run();
  } finally {
    Object.defineProperty(process, 'platform', { value: originalPlatform, configurable: true });
  }
}

beforeEach(() => {
  getFileIconMock.mockClear();
  createFromPathMock.mockClear();
  resizeMock.mockClear();
  createFromBufferMock.mockClear();
  getPackageIconMock.mockClear();
  extractFileIconMock.mockClear();
  existsSyncMock.mockClear();
  readFileSyncMock.mockClear();
  readdirSyncMock.mockClear();
  mkdtempSyncMock.mockClear();
  rmSyncMock.mockClear();
  execFileSyncMock.mockClear();
});

test('uses Windows visual elements assets when the executable icon is unavailable', async () => {
  await expect(getProcessIconDataUrl(WORD_PROCESS_PATH, 'Microsoft Word')).resolves.toBe(
    'data:image/png;base64,word'
  );
  expect(createFromPathMock).toHaveBeenCalledWith(WORD_TARGET_ICON_PATH);
  expect(resizeMock).toHaveBeenCalledWith({ width: 16, height: 16 });
});

test.each([
  ['Microsoft Outlook', OUTLOOK_PROCESS_PATH],
  ['Microsoft Teams', TEAMS_PROCESS_PATH],
  ['GitHub Desktop', GITHUB_DESKTOP_PROCESS_PATH],
  ['Windows App Package Fallback', WINDOWS_APPS_FILE_ICON_PROCESS_PATH]
])(
  'uses the native file icon extractor for %s before Windows fallback assets',
  async (name, path) => {
    await expect(getProcessIconDataUrl(path, name)).resolves.toBe(NATIVE_ICON_DATA_URL);
    expect(extractFileIconMock).toHaveBeenCalledWith(path, 16);
    expect(createFromBufferMock).toHaveBeenCalledWith(NATIVE_ICON_BUFFER);
    expect(createFromPathMock).not.toHaveBeenCalled();
    expect(getFileIconMock).not.toHaveBeenCalled();
  }
);

test.each(WINDOWS_APPS_PACKAGE_ICON_CASES)(
  'uses the Windows package icon for $name installed under WindowsApps',
  async ({ name, processPath, processId }) => {
    await expect(getProcessIconDataUrl(processPath, name, processId)).resolves.toBe(
      WINDOWS_APPS_PACKAGE_ICON_DATA_URL
    );
    expect(getPackageIconMock).toHaveBeenCalledWith(processId, processPath, 16);
    expect(createFromBufferMock).toHaveBeenCalledWith(WINDOWS_APPS_PACKAGE_ICON_BUFFER);
    expect(extractFileIconMock).not.toHaveBeenCalled();
    expect(createFromPathMock).not.toHaveBeenCalled();
    expect(getFileIconMock).not.toHaveBeenCalled();
  }
);

test('uses the Windows package icon without a live process ID', async () => {
  await expect(getProcessIconDataUrl(WINDOWS_APPS_CLAUDE_PROCESS_PATH, 'Claude')).resolves.toBe(
    WINDOWS_APPS_PACKAGE_ICON_DATA_URL
  );
  expect(getPackageIconMock).toHaveBeenCalledWith(0, WINDOWS_APPS_CLAUDE_PROCESS_PATH, 16);
});

test('falls back to the Electron shell icon when no Windows visual elements manifest exists', async () => {
  await expect(getProcessIconDataUrl(SHELL_ICON_PROCESS_PATH, 'Shell Icon App')).resolves.toBe(
    SHELL_ICON_DATA_URL
  );
  expect(getFileIconMock).toHaveBeenCalledWith(SHELL_ICON_PROCESS_PATH, { size: 'small' });
});

test('converts macOS .icns app icons when Electron cannot load them directly', async () => {
  await withPlatform('darwin', async () => {
    await expect(getProcessIconDataUrl(MAC_WORD_PROCESS_PATH, 'Microsoft Word')).resolves.toBe(
      'data:image/png;base64,mac-word'
    );
  });

  expect(createFromPathMock).toHaveBeenCalledWith(MAC_WORD_ICON_PATH);
  expect(execFileSyncMock).toHaveBeenCalledWith(
    '/usr/bin/sips',
    ['-s', 'format', 'png', '--out', SIPS_PNG_PATH, MAC_WORD_ICON_PATH],
    { stdio: 'ignore' }
  );
  expect(createFromBufferMock).toHaveBeenCalledWith(Buffer.from('converted-png'));
  expect(rmSyncMock).toHaveBeenCalledWith(SIPS_TEMP_DIRECTORY, { recursive: true, force: true });
});

test('uses the macOS app bundle with the native file icon extractor', async () => {
  await expect(getProcessIconDataUrl(MAC_TEAMS_PROCESS_PATH, 'Microsoft Teams')).resolves.toBe(
    NATIVE_ICON_DATA_URL
  );
  expect(extractFileIconMock).toHaveBeenCalledWith(MAC_TEAMS_APP_BUNDLE_PATH, 16);
});
