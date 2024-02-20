const { notarize } = require('@electron/notarize');

module.exports = async (context) => {
  if (process.platform !== 'darwin') {
    return;
  }
  console.info('AfterSign hook triggered. Starting notarization step.');
  if (!process.env.CI) {
    console.warn('Skipping notarizing, not running in CI.');
    return;
  }
  if (!('APPLE_ID' in process.env && 'APPLE_APP_SPECIFIC_PASSWORD' in process.env)) {
    console.warn(
      'Skipping notarizing step. APPLE_ID and APPLE_APP_SPECIFIC_PASSWORD env variables must be set.'
    );
    return;
  }

  const appName = context.packager.appInfo.productFilename;
  const { appOutDir } = context;
  try {
    await notarize({
      tool: 'notarytool',
      appPath: `${appOutDir}/${appName}.app`,
      appleId: process.env.APPLE_ID,
      appleIdPassword: process.env.APPLE_APP_SPECIFIC_PASSWORD,
      teamId: process.env.APPLE_TEAM_ID
    });
  } catch (error) {
    console.error(error);
  }

  console.log(`Done notarizing.`);
};
