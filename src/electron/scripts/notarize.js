const { notarize } = require("@electron/notarize");

exports.default = async function notarizeMacos(context) {
  const { electronPlatformName, appOutDir } = context;
  if (electronPlatformName !== "darwin") {
    return;
  }
  console.info("Starting notarization step.");

  if (!process.env.CI) {
    console.warn("Skipping notarizing step. Packaging is not running in CI");
    return;
  }

  if (
    !("APPLE_ID" in process.env && "APPLE_APP_SPECIFIC_PASSWORD" in process.env)
  ) {
    console.warn(
      "Skipping notarizing step. APPLE_ID and APPLE_APP_SPECIFIC_PASSWORD env variables must be set",
    );
    return;
  }

  const appName = context.packager.appInfo.productFilename;

  // These environment variables are set in the GitHub Actions secrets
  await notarize({
    tool: "notarytool",
    appPath: `${appOutDir}/${appName}.app`,
    appleId: process.env.APPLE_ID,
    appleIdPassword: process.env.APPLE_APP_SPECIFIC_PASSWORD,
    teamId: process.env.APPLE_TEAM_ID,
  });
};