## Development

This project uses cocoapods to install dependencies.

- Download cocoapods (`brew install cocoapods` using homebrew).
- run `pod install` (from the AM.PA.MacOs directory)
- Open the project using the .xcworkspace that is created

## First start

The app will check for it's permissions and if it doesn't have any it will cause
a pop up to appear and tell you which ones it needs.

It will also ask for a network connection. This listens only locally and *can
be* encrypted but is currently not. Please allow it will be listening on
`localhost:8765`
