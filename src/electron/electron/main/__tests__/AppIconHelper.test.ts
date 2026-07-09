import { beforeEach, expect, jest, test } from '@jest/globals';

const WORD_PROCESS_PATH = 'C:\\Program Files\\Microsoft Office\\root\\Office16\\WINWORD.EXE';
const WORD_MANIFEST_PATH =
  'C:\\Program Files\\Microsoft Office\\root\\Office16\\WINWORD.VisualElementsManifest.xml';
const WORD_ASSETS_DIRECTORY = 'C:\\Program Files\\Microsoft Office\\root\\Office16\\Assets';
const WORD_TARGET_ICON_PATH =
  'C:\\Program Files\\Microsoft Office\\root\\Office16\\Assets\\WordLogo.targetsize-32.png';
const SHELL_ICON_PROCESS_PATH = 'C:\\Tools\\ShellIconApp\\ShellIconApp.exe';
const SHELL_ICON_DATA_URL = 'data:image/png;base64,shell';
const MAC_WORD_PROCESS_PATH = '/Applications/Microsoft Word.app/Contents/MacOS/Microsoft Word';
const MAC_WORD_INFO_PLIST_PATH = '/Applications/Microsoft Word.app/Contents/Info.plist';
const MAC_WORD_RESOURCES_PATH = '/Applications/Microsoft Word.app/Contents/Resources';
const MAC_WORD_ICON_PATH = '/Applications/Microsoft Word.app/Contents/Resources/Word_macOS.icns';
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

const createFromBufferResizeMock = jest.fn(() => ({
  toDataURL: () => 'data:image/png;base64,mac-word'
}));

const createFromBufferMock = jest.fn(() => ({
  isEmpty: () => false,
  resize: createFromBufferResizeMock
}));

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

const { getProcessIconDataUrl } = await import('../services/utils/AppIconHelper');

beforeEach(() => {
  getFileIconMock.mockClear();
  createFromPathMock.mockClear();
  resizeMock.mockClear();
  createFromBufferMock.mockClear();
  createFromBufferResizeMock.mockClear();
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
  expect(resizeMock).toHaveBeenCalledWith({ width: 64, height: 64 });
});

test('falls back to the Electron shell icon when no Windows visual elements manifest exists', async () => {
  await expect(getProcessIconDataUrl(SHELL_ICON_PROCESS_PATH, 'Shell Icon App')).resolves.toBe(
    SHELL_ICON_DATA_URL
  );
  expect(getFileIconMock).toHaveBeenCalledWith(SHELL_ICON_PROCESS_PATH, { size: 'small' });
});

test('converts macOS icns app icons when Electron cannot load them directly', async () => {
  await expect(getProcessIconDataUrl(MAC_WORD_PROCESS_PATH, 'Microsoft Word')).resolves.toBe(
    'data:image/png;base64,mac-word'
  );
  expect(createFromPathMock).toHaveBeenCalledWith(MAC_WORD_ICON_PATH);
  expect(execFileSyncMock).toHaveBeenCalledWith(
    '/usr/bin/sips',
    ['-s', 'format', 'png', '--out', SIPS_PNG_PATH, MAC_WORD_ICON_PATH],
    { stdio: 'ignore' }
  );
  expect(createFromBufferMock).toHaveBeenCalledWith(Buffer.from('converted-png'));
  expect(createFromBufferResizeMock).toHaveBeenCalledWith({ width: 64, height: 64 });
  expect(rmSyncMock).toHaveBeenCalledWith(SIPS_TEMP_DIRECTORY, { recursive: true, force: true });
});
