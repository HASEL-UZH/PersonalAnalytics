import { app, nativeImage } from 'electron';
import extractFileIcon from 'extract-file-icon';
import { execFileSync } from 'node:child_process';
import { existsSync, mkdtempSync, readFileSync, readdirSync, rmSync } from 'node:fs';
import { tmpdir } from 'node:os';
import path from 'node:path';
import { getMainLogger } from '../../../config/Logger';

const LOG = getMainLogger('AppIconHelper');
const APP_ICON_DATA_URL_CACHE = new Map<string, Promise<string | undefined>>();
const APP_ICON_SIZE = 16;
const WINDOWS_LOGO_ATTRIBUTE_PRIORITY = [
  'Square44x44Logo',
  'SmallLogo',
  'Square30x30Logo',
  'Square32x32Logo',
  'Square70x70Logo',
  'Square71x71Logo',
  'Square150x150Logo',
  'Logo'
];
const WINDOWS_ASSET_VARIANT_PRIORITY = [
  'targetsize-64',
  'targetsize-48',
  'targetsize-44',
  'targetsize-40',
  'targetsize-32',
  'targetsize-30',
  'targetsize-24',
  'targetsize-20',
  'targetsize-16',
  'scale-100',
  'scale-125',
  'scale-150',
  'scale-200',
  'scale-400'
];
const WINDOWS_IMAGE_EXTENSIONS = new Set(['.ico', '.jpg', '.jpeg', '.png']);

function normalizeIconName(value: string): string {
  return value.toLowerCase().replace(/[^a-z0-9]+/g, '');
}

function getMacAppBundlePath(processPath: string | null): string | null {
  if (!processPath) {
    return null;
  }

  const appBundleMatch = processPath.match(/^(.+?\.app)(?:\/|$)/);
  return appBundleMatch?.[1] || null;
}

function isWindowsProcessPath(processPath: string): boolean {
  return /^[a-zA-Z]:[\\/]/.test(processPath) || processPath.includes('\\');
}

function isWindowsAppsProcessPath(processPath: string): boolean {
  return isWindowsProcessPath(processPath) && /[\\/]WindowsApps[\\/]/i.test(processPath);
}

function getPathModuleForProcessPath(processPath: string) {
  return isWindowsProcessPath(processPath) ? path.win32 : path;
}

function uniqueStrings(values: (string | undefined)[]): string[] {
  const seenValues = new Set<string>();
  const uniqueValues: string[] = [];

  values.forEach((value) => {
    const trimmedValue = value?.trim();
    if (!trimmedValue || seenValues.has(trimmedValue)) {
      return;
    }

    seenValues.add(trimmedValue);
    uniqueValues.push(trimmedValue);
  });

  return uniqueValues;
}

function decodeXmlAttributeValue(value: string): string {
  return value
    .replace(/&quot;/g, '"')
    .replace(/&apos;/g, "'")
    .replace(/&lt;/g, '<')
    .replace(/&gt;/g, '>')
    .replace(/&amp;/g, '&');
}

function getXmlAttributeValue(xml: string, attributeName: string): string | undefined {
  const attributeMatch = xml.match(new RegExp(`\\b${attributeName}\\s*=\\s*["']([^"']+)["']`, 'i'));
  return attributeMatch?.[1] ? decodeXmlAttributeValue(attributeMatch[1]) : undefined;
}

function getWindowsVisualElementsManifestPaths(processPath: string | null): string[] {
  if (!processPath || !isWindowsProcessPath(processPath)) {
    return [];
  }

  const pathModule = getPathModuleForProcessPath(processPath);
  const processPathParts = pathModule.parse(processPath);
  if (!processPathParts.dir || !processPathParts.name) {
    return [];
  }

  return uniqueStrings([
    pathModule.join(processPathParts.dir, `${processPathParts.name}.VisualElementsManifest.xml`),
    pathModule.join(
      processPathParts.dir,
      `${processPathParts.name.toUpperCase()}.VisualElementsManifest.xml`
    ),
    pathModule.join(
      processPathParts.dir,
      `${processPathParts.name.toLowerCase()}.VisualElementsManifest.xml`
    )
  ]);
}

function getWindowsManifestLogoPaths(manifestPath: string): string[] {
  try {
    const manifest = readFileSync(manifestPath, 'utf8');
    const prioritizedLogoPaths = WINDOWS_LOGO_ATTRIBUTE_PRIORITY.map((attributeName) =>
      getXmlAttributeValue(manifest, attributeName)
    );
    const discoveredLogoPaths = Array.from(
      manifest.matchAll(/\b((?:[A-Za-z_][\w.-]*:)?[A-Za-z0-9]+Logo)\s*=\s*["']([^"']+)["']/gi)
    )
      .filter(([, attributeName]) => {
        return !attributeName.split(':').pop()?.toLowerCase().startsWith('shownameon');
      })
      .map(([, , value]) => decodeXmlAttributeValue(value));

    return uniqueStrings([...prioritizedLogoPaths, ...discoveredLogoPaths]);
  } catch (error) {
    LOG.debug('Could not read Windows visual elements manifest', manifestPath, error);
    return [];
  }
}

function getWindowsAssetVariantRank(fileName: string): number {
  const normalizedFileName = fileName.toLowerCase();
  const priorityIndex = WINDOWS_ASSET_VARIANT_PRIORITY.findIndex((priorityFragment) =>
    normalizedFileName.includes(priorityFragment)
  );

  return priorityIndex === -1 ? WINDOWS_ASSET_VARIANT_PRIORITY.length : priorityIndex;
}

function getWindowsLogoCandidatePaths(manifestPath: string, logoPath: string): string[] {
  const pathModule = getPathModuleForProcessPath(manifestPath);
  const manifestDirectory = pathModule.dirname(manifestPath);
  const absoluteLogoPath = pathModule.resolve(manifestDirectory, logoPath);
  const logoPathParts = pathModule.parse(absoluteLogoPath);
  const candidatePaths = [absoluteLogoPath];

  if (!logoPathParts.dir || !existsSync(logoPathParts.dir)) {
    return candidatePaths;
  }

  try {
    const logoBaseName = logoPathParts.name.toLowerCase();
    const logoExtension = logoPathParts.ext.toLowerCase();
    const variantFiles = readdirSync(logoPathParts.dir)
      .filter((file) => {
        const fileParts = pathModule.parse(file);
        const fileBaseName = fileParts.name.toLowerCase();
        const fileExtension = fileParts.ext.toLowerCase();
        const hasExpectedExtension = logoExtension
          ? fileExtension === logoExtension
          : WINDOWS_IMAGE_EXTENSIONS.has(fileExtension);

        return (
          hasExpectedExtension &&
          (fileBaseName === logoBaseName || fileBaseName.startsWith(`${logoBaseName}.`))
        );
      })
      .sort((firstFile, secondFile) => {
        const rankDifference =
          getWindowsAssetVariantRank(firstFile) - getWindowsAssetVariantRank(secondFile);
        return rankDifference || firstFile.localeCompare(secondFile);
      });

    candidatePaths.push(...variantFiles.map((file) => pathModule.join(logoPathParts.dir, file)));
  } catch (error) {
    LOG.debug('Could not read Windows visual elements assets', logoPathParts.dir, error);
  }

  return uniqueStrings(candidatePaths);
}

function getWindowsVisualElementsIconDataUrl(processPath: string | null): string | undefined {
  for (const manifestPath of getWindowsVisualElementsManifestPaths(processPath)) {
    if (!existsSync(manifestPath)) {
      continue;
    }

    for (const logoPath of getWindowsManifestLogoPaths(manifestPath)) {
      for (const candidatePath of getWindowsLogoCandidatePaths(manifestPath, logoPath)) {
        const iconDataUrl = getIconDataUrlFromPath(candidatePath);
        if (iconDataUrl) {
          return iconDataUrl;
        }
      }
    }
  }

  return undefined;
}

function getPersonalAnalyticsIconDataUrl(): string | undefined {
  const iconPath = path.join(
    process.env.VITE_PUBLIC || process.env.DIST || '',
    'IconColored@2x.png'
  );
  return getIconDataUrlFromPath(iconPath);
}

function getPlistIconFileName(infoPlistPath: string): string | undefined {
  if (!existsSync(infoPlistPath)) {
    return undefined;
  }

  try {
    const infoPlist = readFileSync(infoPlistPath, 'utf8');
    const iconMatch = infoPlist.match(/<key>CFBundleIconFile<\/key>\s*<string>([^<]+)<\/string>/);
    return iconMatch?.[1];
  } catch (error) {
    LOG.debug('Could not read app icon plist', infoPlistPath, error);
    return undefined;
  }
}

function getIconDataUrlFromPath(iconPath: string | undefined): string | undefined {
  if (!iconPath || !existsSync(iconPath)) {
    return undefined;
  }

  const icon = nativeImage.createFromPath(iconPath);
  if (icon.isEmpty()) {
    if (process.platform === 'darwin' && path.extname(iconPath).toLowerCase() === '.icns') {
      return getMacIcnsDataUrlViaSips(iconPath);
    }

    return undefined;
  }

  return icon.resize({ width: APP_ICON_SIZE, height: APP_ICON_SIZE }).toDataURL();
}

function getNativeFileIconDataUrl(iconPath: string | undefined): string | undefined {
  if (!iconPath) {
    return undefined;
  }

  try {
    const iconBuffer = extractFileIcon(iconPath, APP_ICON_SIZE);
    if (!iconBuffer?.length) {
      return undefined;
    }

    const icon = nativeImage.createFromBuffer(iconBuffer);
    if (icon.isEmpty()) {
      return undefined;
    }

    return icon.resize({ width: APP_ICON_SIZE, height: APP_ICON_SIZE }).toDataURL();
  } catch (error) {
    LOG.debug('Could not extract native app icon', iconPath, error);
    return undefined;
  }
}

function getWindowsAppsPackageIconDataUrl(
  processPath: string | null,
  processId: number | null
): string | undefined {
  if (!processPath || !isWindowsAppsProcessPath(processPath)) {
    return undefined;
  }

  try {
    const iconBuffer = extractFileIcon.getPackageIcon?.(processId ?? 0, processPath, APP_ICON_SIZE);
    if (!iconBuffer?.length) {
      return undefined;
    }

    const icon = nativeImage.createFromBuffer(iconBuffer);
    if (icon.isEmpty()) {
      return undefined;
    }

    return icon.resize({ width: APP_ICON_SIZE, height: APP_ICON_SIZE }).toDataURL();
  } catch (error) {
    LOG.debug('Could not extract Windows app package icon', processPath, error);
    return undefined;
  }
}

function getMacIcnsDataUrlViaSips(iconPath: string): string | undefined {
  const tempDirectory = mkdtempSync(path.join(tmpdir(), 'personal-analytics-icon-'));
  const pngPath = path.join(tempDirectory, 'icon.png');

  try {
    execFileSync('/usr/bin/sips', ['-s', 'format', 'png', '--out', pngPath, iconPath], {
      stdio: 'ignore'
    });

    const icon = nativeImage.createFromBuffer(readFileSync(pngPath));
    if (icon.isEmpty()) {
      return undefined;
    }

    return icon.resize({ width: APP_ICON_SIZE, height: APP_ICON_SIZE }).toDataURL();
  } catch (error) {
    LOG.debug('Could not convert macOS icns app icon', iconPath, error);
    return undefined;
  } finally {
    rmSync(tempDirectory, { recursive: true, force: true });
  }
}

function getBundleResourceIconDataUrl(
  appBundlePath: string,
  processName: string | null
): string | undefined {
  const resourcesPath = path.join(appBundlePath, 'Contents', 'Resources');
  if (!existsSync(resourcesPath)) {
    return undefined;
  }

  const resourceFiles = readdirSync(resourcesPath).filter((file) => /\.(?:icns|png)$/i.test(file));
  const normalizedProcessName = processName ? normalizeIconName(processName) : '';
  const processSpecificFiles = normalizedProcessName
    ? resourceFiles.filter((file) => {
        const normalizedFile = normalizeIconName(path.parse(file).name);
        return (
          normalizedFile.includes(normalizedProcessName) && !normalizedFile.includes('template')
        );
      })
    : [];

  const plistIconFile = getPlistIconFileName(path.join(appBundlePath, 'Contents', 'Info.plist'));
  const plistIconCandidates = plistIconFile
    ? [
        plistIconFile,
        path.extname(plistIconFile) ? plistIconFile : `${plistIconFile}.icns`,
        path.extname(plistIconFile) ? plistIconFile : `${plistIconFile}.png`
      ]
    : [];

  const candidateFiles = [
    ...processSpecificFiles,
    'app.icns',
    'icon.icns',
    'icon.png',
    'Icon.icns',
    'Icon.png',
    'AppIcon.icns',
    'AppIcon.png',
    ...plistIconCandidates,
    ...resourceFiles.filter((file) => !normalizeIconName(file).includes('template'))
  ];

  const seenFiles = new Set<string>();
  for (const file of candidateFiles) {
    if (seenFiles.has(file)) {
      continue;
    }
    seenFiles.add(file);

    const iconDataUrl = getIconDataUrlFromPath(path.join(resourcesPath, file));
    if (iconDataUrl) {
      return iconDataUrl;
    }
  }

  return undefined;
}

/**
 * Resolves a local app icon for a tracked process and returns it as a small renderer-safe data URL.
 */
export async function getProcessIconDataUrl(
  processPath: string | null,
  processName: string | null,
  processId: number | null = null
): Promise<string | undefined> {
  const cacheKey = `${processPath || ''}\u0000${processName || ''}\u0000${processId ?? ''}`;
  const cachedIcon = APP_ICON_DATA_URL_CACHE.get(cacheKey);
  if (cachedIcon) {
    return cachedIcon;
  }

  const iconPromise = (async () => {
    const appBundlePath = getMacAppBundlePath(processPath);
    if (
      processName === 'Electron' &&
      processPath?.includes('/PersonalAnalytics/') &&
      processPath.includes('/node_modules/electron/')
    ) {
      const personalAnalyticsIcon = getPersonalAnalyticsIconDataUrl();
      if (personalAnalyticsIcon) {
        return personalAnalyticsIcon;
      }
    }

    const windowsAppsPackageIcon = getWindowsAppsPackageIconDataUrl(processPath, processId);
    if (windowsAppsPackageIcon) {
      return windowsAppsPackageIcon;
    }

    const nativeFileIcon = getNativeFileIconDataUrl(appBundlePath || processPath);
    if (nativeFileIcon) {
      return nativeFileIcon;
    }

    if (appBundlePath) {
      const bundleIcon = getBundleResourceIconDataUrl(appBundlePath, processName);
      if (bundleIcon) {
        return bundleIcon;
      }
    }

    if (!processPath) {
      return undefined;
    }

    const windowsVisualElementsIcon = getWindowsVisualElementsIconDataUrl(processPath);
    if (windowsVisualElementsIcon) {
      return windowsVisualElementsIcon;
    }

    return app
      .getFileIcon(appBundlePath || processPath, { size: 'small' })
      .then((icon) => {
        if (icon.isEmpty()) {
          return undefined;
        }
        return icon.resize({ width: APP_ICON_SIZE, height: APP_ICON_SIZE }).toDataURL();
      })
      .catch((error) => {
        LOG.debug('Could not load app icon', processPath, error);
        return undefined;
      });
  })();

  APP_ICON_DATA_URL_CACHE.set(cacheKey, iconPromise);
  return iconPromise;
}
