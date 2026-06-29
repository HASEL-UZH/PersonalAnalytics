import { app, nativeImage } from 'electron';
import { existsSync, readFileSync, readdirSync } from 'node:fs';
import path from 'node:path';
import { getMainLogger } from '../../../config/Logger';

const LOG = getMainLogger('AppIconHelper');
const APP_ICON_DATA_URL_CACHE = new Map<string, Promise<string | undefined>>();
const APP_ICON_SIZE = 16;

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
    return undefined;
  }

  return icon.resize({ width: APP_ICON_SIZE, height: APP_ICON_SIZE }).toDataURL();
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
  processName: string | null
): Promise<string | undefined> {
  const cacheKey = `${processPath || ''}\u0000${processName || ''}`;
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

    if (appBundlePath) {
      const bundleIcon = getBundleResourceIconDataUrl(appBundlePath, processName);
      if (bundleIcon) {
        return bundleIcon;
      }
    }

    if (!processPath) {
      return undefined;
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
