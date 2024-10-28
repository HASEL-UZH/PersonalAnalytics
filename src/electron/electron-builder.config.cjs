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
    owner: 'HASEL-UZH',
    repo: 'PersonalAnalytics'
  },
  mac: {
    artifactName: '${productName}-${version}-${env.MAC_ARCH_TYPE}.${ext}',
    asarUnpack: ['node_modules/**/*.node'],
    entitlements: 'build/entitlements.mac.plist',
    entitlementsInherit: 'build/entitlements.mac.plist',
    hardenedRuntime: true,
    gatekeeperAssess: false,
    notarize: {
      teamId: `${process.env.APPLE_TEAM_ID}`
    },
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
    artifactName: '${productName}-${version}-Windows.${ext}'
  },
  nsis: {
    oneClick: true,
    deleteAppDataOnUninstall: false,
    differentialPackage: false
  }
};
