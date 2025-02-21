module.exports = {
  productName: 'PersonalAnalytics',
  appId: 'ch.ifi.hasel.personal-analytics',
  asar: true,
  asarUnpack: ['node_modules/better_sqlite3/**', 'node_modules/sqlite3/**'],
  directories: {
    output: 'release/${version}'
  },
  files: ['dist', 'dist-electron'],
  publish: {
    provider: 'github',
    owner: 'SWELL-PA-DEV',
    repo: 'PersonalAnalytics-SWELL'
  },
  afterSign: "scripts/notarize.cjs",
  mac: {
    target: {
      target: "default",
      arch: ["x64", "arm64"],
    },
    artifactName: '${productName}-${version}-${arch}.${ext}',
    asarUnpack: ['node_modules/**/*.node'],
    entitlements: 'build/entitlements.mac.plist',
    entitlementsInherit: 'build/entitlements.mac.plist',
    hardenedRuntime: true,
    gatekeeperAssess: false,
    notarize: false,
    extendInfo: [
      {
        key: 'NSAppleEventsUsageDescription',
        value: 'Please allow access to use the application.'
      },
      {
        key: 'NSDocumentsFolderUsageDescription',
        value: 'Please allow access to use the application.'
      },
      {
        key: 'NSDownloadsFolderUsageDescription',
        value: 'Please allow access to use the application.'
      }
    ]
  },
  dmg: {
    writeUpdateInfo: false
  },
  win: {
    target: ["nsis"],
    verifyUpdateCodeSignature: false,
    azureSignOptions: {
      publisherName: `${process.env.AZURE_PUBLISHER_NAME}`,
      endpoint: `${process.env.AZURE_ENDPOINT}`,
      codeSigningAccountName: `${process.env.AZURE_CODE_SIGNING_NAME}`,
      certificateProfileName: `${process.env.AZURE_CERT_PROFILE_NAME}`,
    },
  },
  nsis: {
    oneClick: true,
    deleteAppDataOnUninstall: false,
    differentialPackage: false,
    artifactName: '${productName}-${version}-Windows.${ext}',
  }
};
