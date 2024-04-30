# Troubleshooting PersonalAnalytics

## macOS: Reinstalling PersonalAnalytics from Scratch
> If you are experiencing issues with PersonalAnalytics on macOS and want to reinstall the app, please follow the steps below:

1. Quit PA and check that it is not running with:
```
ps aux | grep -i personal
```

2. Remove permissions for PA in System Settings:
```
open "x-apple.systempreferences:com.apple.preference.security”
```

Check for entries in “Accessibility” and “Screen & System Audio Recording”, and delete these entries using the “-“ symbol at the bottom of the list.

> Note: Checking for these permissions needs to be done *BEFORE* deleting the PA app, as permissions for deleted apps are kept, but not shown in this list if the app is deleted.

3. Delete PA and its settings

- Copy the database if you want to keep it
```
open ~/Library/Application\ Support/personal-analytics
```
- Delete PA
```
rm -rf /Applications/PersonalAnalytics.app
```
- Delete Settings
```
rm -rf ~/Library/Application\ Support/personal-analytics
```

4. Make sure PA and settings are deleted
```
ls -lah /Applications | grep -i personal
ls -lah ~/Library/Application\ Support | grep -i personal
```

5. Done - you can freshly install PersonalAnalytics now!
